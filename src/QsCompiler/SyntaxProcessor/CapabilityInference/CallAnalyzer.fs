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

module Recursion =
    let callableSet (cycles: #(CallGraphNode seq) seq) =
        Seq.collect id cycles |> Seq.map (fun n -> n.CallableName) |> Set.ofSeq

type DeepCallAnalyzer(callables: ImmutableDictionary<_, QsCallable>, graph: CallGraph, syntaxAnalyzer: Analyzer<_, _>) =
    static let createPattern capability =
        {
            Capability = capability
            Diagnose = fun _ -> None
            Properties = ()
        }

    let findCallable (node: CallGraphNode) =
        callables.TryGetValue node.CallableName |> tryOption

    let syntaxPatterns =
        callables
        |> Seq.filter (fun c -> QsNullable.isNull c.Value.Source.AssemblyFile)
        |> Seq.map (fun c -> c.Key, syntaxAnalyzer c.Value |> Seq.toList)
        |> readOnlyDict

    let cycles =
        graph.GetCallCycles()
        |> Seq.filter (findCallable >> Option.exists (fun c -> QsNullable.isNull c.Source.AssemblyFile) |> Seq.exists)

    let cycleCapabilities = Dictionary()
    let callablePatterns = Dictionary()
    let visitedCallables = HashSet()

    do
        for cycle in cycles do
            let capability = Seq.choose findCallable cycle |> Seq.collect syntaxAnalyzer |> Pattern.concat

            for node in cycle do
                cycleCapabilities[node.CallableName] <- tryOption (cycleCapabilities.TryGetValue node.CallableName)
                                                        |> Option.fold RuntimeCapability.merge capability

    member analyzer.Analyze(callable: QsCallable) =
        let storePatterns () =
            if visitedCallables.Add callable.FullName then
                let patterns = analyzer.CallablePatterns callable
                callablePatterns[callable.FullName] <- patterns
                patterns
            else
                []

        callablePatterns.TryGetValue callable.FullName |> tryOption |> Option.defaultWith storePatterns

    member private analyzer.CallablePatterns callable =
        match SymbolResolution.TryGetRequiredCapability callable.Attributes with
        | Value capability -> [ createPattern capability ]
        | Null when QsNullable.isNull callable.Source.AssemblyFile ->
            [
                match syntaxPatterns.TryGetValue callable.FullName with
                | true, patterns -> yield! patterns
                | false, _ -> ()

                match cycleCapabilities.TryGetValue callable.FullName with
                | true, capability -> createPattern capability
                | false, _ -> ()

                analyzer.DependentCapability callable.FullName |> createPattern
            ]
        | Null -> []

    member private analyzer.DependentCapability name =
        graph.GetDirectDependencies(CallGraphNode name)
        |> Seq.choose (fun group -> findCallable group.Key)
        |> Seq.collect analyzer.Analyze
        |> Pattern.concat

type Call = { Name: QsQualifiedName; Range: Range QsNullable }

module CallAnalyzer =
    let createPattern capability (name: QsQualifiedName) range =
        let diagnose (target: Target) =
            let range = QsNullable.defaultValue Range.Zero range

            if RuntimeCapability.subsumes target.Capability capability then
                None
            else
                let args =
                    [
                        name.Name
                        target.Name
                        string capability.ResultOpacity
                        string capability.Classical
                    ]

                QsCompilerDiagnostic.Error(ErrorCode.UnsupportedCallableCapability, args) range |> Some

        {
            Capability = capability
            Diagnose = diagnose
            Properties = { Name = name; Range = range }
        }

    let globalCallableIds action =
        let transformation = LocatingTransformation TransformationOptions.NoRebuild
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

    let shallow (nsManager: NamespaceManager) (graph: CallGraph) (node: CallGraphNode) =
        let offset, source =
            match nsManager.TryGetCallable node.CallableName ("", "") with
            | Found { Position = DeclarationHeader.Defined offset; Source = source } -> offset, source
            | _ -> failwith "Callable not found."

        let dependencies =
            if QsNullable.isNull source.AssemblyFile then
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

        seq {
            for name, range in dependencies do
                match capabilityAttribute name with
                | Value capability -> createPattern capability name range
                | Null -> ()
        }

    let deep callables graph syntaxAnalyzer : Analyzer<_, _> =
        let analyzer = DeepCallAnalyzer(callables, graph, syntaxAnalyzer)
        fun callable -> analyzer.Analyze callable
