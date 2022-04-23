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

type DeepCallKind =
    | Attribute
    | Dependency

type DeepCallPattern =
    {
        Kind: DeepCallKind
        Capability: RuntimeCapability
    }

    interface IPattern with
        member pattern.Capability = pattern.Capability

        member _.Diagnose _ = None

module CallAnalyzer =
    let globalReferences action =
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

    let shallow (nsManager: NamespaceManager) (graph: CallGraph) node =
        let dependencies = graph.GetDirectDependencies node

        let codeFile, isInReference, offset =
            match nsManager.TryGetCallable node.CallableName ("", "") with
            | Found ({ Position = DeclarationHeader.Defined p } as callable) ->
                callable.Source.CodeFile, QsNullable.isValue callable.Source.AssemblyFile, p
            | _ -> failwith "Callable not found."

        let references =
            if isInReference then
                nsManager.ImportedSpecializations node.CallableName
                |> Seq.collect (fun (_, spec) ->
                    globalReferences (fun t -> t.Namespaces.OnSpecializationImplementation spec |> ignore))
            else
                dependencies
                |> Seq.collect (fun group ->
                    group |> Seq.map (fun edge -> group.Key.CallableName, offset + edge.ReferenceRange |> Value))

        seq {
            for name, range in references do
                match nsManager.TryGetCallable name (node.CallableName.Namespace, codeFile) with
                | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
                    match SymbolResolution.TryGetRequiredCapability callable.Attributes with
                    | Value capability ->
                        {
                            kind = External capability
                            name = name
                            range = range
                        }
                    | Null -> ()
                | _ -> ()

            for cycle in graph.GetCallCycles() |> Seq.filter (Seq.contains node) do
                for node in Seq.filter dependencies.Contains cycle do
                    for edge in dependencies[node] do
                        {
                            kind = Recursive
                            name = node.CallableName
                            range = offset + edge.ReferenceRange |> Value
                        }
        }

    let declaredInSource (callable: QsCallable) =
        QsNullable.isNull callable.Source.AssemblyFile

    let deep (callables: ImmutableDictionary<_, _>) (graph: CallGraph) (syntaxAnalyzer: Analyzer<_, _>) =
        let findCallable (node: CallGraphNode) =
            callables.TryGetValue node.CallableName |> tryOption

        let syntaxPatterns =
            callables
            |> Seq.filter (fun c -> declaredInSource c.Value)
            |> Seq.map (fun c -> c.Key, syntaxAnalyzer c.Value |> Seq.toList)
            |> readOnlyDict

        let cycles =
            graph.GetCallCycles() |> Seq.filter (findCallable >> Option.exists declaredInSource |> Seq.exists)

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
                    | Value capability -> seq { { Kind = Attribute; Capability = capability } }
                    | Null when declaredInSource callable ->
                        seq {
                            match syntaxPatterns.TryGetValue callable.FullName with
                            | true, patterns -> yield! patterns
                            | false, _ -> ()

                            match cycleCapabilities.TryGetValue callable.FullName with
                            | true, capability -> { Kind = Dependency; Capability = capability }
                            | false, _ -> ()

                            { Kind = Dependency; Capability = dependentCapability visited callable.FullName }
                        }
                    | Null -> Seq.empty

                cache[callable.FullName] <- patterns
                patterns

        callablePatterns Set.empty
