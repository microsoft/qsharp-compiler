// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System.Collections.Generic
open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Utils

type CallKind =
    | External of RuntimeCapability
    | Recursive

type CallPattern =
    {
        kind: CallKind
        name: QsQualifiedName
        range: Range QsNullable
    }

    interface IPattern with
        member pattern.Capability =
            match pattern.kind with
            | External capability -> capability
            | Recursive -> RuntimeCapability.Base // TODO

        member pattern.Diagnose target =
            match pattern.kind with
            | External capability ->
                if target.Capability.Implies capability then
                    None
                else
                    let args = [ pattern.Name.Name; string capability; target.Architecture ]
                    let range = pattern.Range.ValueOr Range.Zero
                    QsCompilerDiagnostic.Error(ErrorCode.UnsupportedCallableCapability, args) range |> Some
            | Recursive -> None // TODO

    member pattern.Name = pattern.name

    member pattern.Range = pattern.range

module CallPattern =
    let create kind name range =
        {
            kind = kind
            name = name
            range = range
        }

type TransitiveCallPattern =
    | TransitiveCallPattern of RuntimeCapability

    interface IPattern with
        member pattern.Capability =
            let (TransitiveCallPattern capability) = pattern
            capability

        member _.Diagnose _ = None

module CallAnalyzer =
    let globalCallableIds action =
        let transformation = LocationTrackingTransformation TransformationOptions.NoRebuild
        let references = ResizeArray()

        transformation.Expressions <-
            { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
                override this.OnTypedExpression expression =
                    match expression.Expression with
                    | Identifier (GlobalCallable name, _) ->
                        let range = QsNullable.Map2(+) transformation.Offset expression.Range
                        references.Add(name, range)
                    | _ -> ()

                    base.OnTypedExpression expression
            }

        action transformation
        references

    let isLocal source = QsNullable.isNull source.AssemblyFile

    let shallow (nsManager: NamespaceManager) (graph: CallGraph) (node: CallGraphNode) =
        let offset, source =
            match nsManager.TryGetCallable node.CallableName ("", "") with
            | Found { Position = DeclarationHeader.Defined offset; Source = source } -> offset, source
            | _ -> failwith "Callable not found."

        let dependencies =
            if isLocal source then
                graph.GetDirectDependencies node
                |> Seq.collect (fun group ->
                    group |> Seq.map (fun edge -> group.Key.CallableName, offset + edge.ReferenceRange |> Value))
            else
                nsManager.ImportedSpecializations node.CallableName
                |> Seq.collect (fun (_, spec) ->
                    globalCallableIds (fun t -> t.Namespaces.OnSpecializationImplementation spec |> ignore))

        let capabilityAttribute name =
            match nsManager.TryGetCallable name (node.CallableName.Namespace, source.CodeFile) with
            | Found callable -> SymbolResolution.TryGetRequiredCapability callable.Attributes
            | _ -> Null

        let callablesInCycle =
            graph.GetCallCycles() |> Seq.collect id |> Seq.map (fun n -> n.CallableName) |> Set.ofSeq

        seq {
            for name, range in dependencies do
                match capabilityAttribute name with
                | Value capability -> CallPattern.create (External capability) name range
                | Null -> ()

                if Set.contains name callablesInCycle then CallPattern.create Recursive node.CallableName range
        }

    let deep (callables: ImmutableDictionary<_, QsCallable>) (graph: CallGraph) (syntaxAnalyzer: Analyzer<_, _>) =
        let findCallable (node: CallGraphNode) =
            callables.TryGetValue node.CallableName |> tryOption

        let syntaxPatterns =
            callables
            |> Seq.filter (fun c -> isLocal c.Value.Source)
            |> Seq.map (fun c -> c.Key, syntaxAnalyzer c.Value |> Seq.toList)
            |> readOnlyDict

        let cycles =
            graph.GetCallCycles()
            |> Seq.filter (findCallable >> Option.exists (fun c -> isLocal c.Source) |> Seq.exists)

        let cycleCapabilities = Dictionary()

        for cycle in cycles do
            let capability =
                Seq.choose findCallable cycle
                |> Seq.collect syntaxAnalyzer
                |> Seq.map (fun p -> p.Capability)
                |> Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

            for node in cycle do
                cycleCapabilities[node.CallableName] <- tryOption (cycleCapabilities.TryGetValue node.CallableName)
                                                        |> Option.fold RuntimeCapability.Combine capability

        let cache = Dictionary()

        let rec dependentCapability visited name =
            let visited = Set.add name visited

            graph.GetDirectDependencies(CallGraphNode name)
            |> Seq.map (fun group -> group.Key)
            |> Seq.filter (fun node -> Set.contains node.CallableName visited |> not)
            |> Seq.choose findCallable
            |> Seq.collect (callablePatterns visited)
            |> Seq.map (fun p -> p.Capability)
            |> Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

        and callablePatterns visited callable : IPattern seq =
            match cache.TryGetValue callable.FullName with
            | true, patterns -> patterns
            | false, _ ->
                let patterns: IPattern seq =
                    match SymbolResolution.TryGetRequiredCapability callable.Attributes with
                    | Value capability -> seq { TransitiveCallPattern capability }
                    | Null when isLocal callable.Source ->
                        seq {
                            match syntaxPatterns.TryGetValue callable.FullName with
                            | true, patterns -> yield! patterns
                            | false, _ -> ()

                            match cycleCapabilities.TryGetValue callable.FullName with
                            | true, capability -> TransitiveCallPattern capability
                            | false, _ -> ()

                            dependentCapability visited callable.FullName |> TransitiveCallPattern
                        }
                    | Null -> Seq.empty

                cache[callable.FullName] <- patterns
                patterns

        callablePatterns Set.empty
