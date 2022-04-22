// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.CallAnalyzer

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// Returns a list of the names of global callables referenced in the scope, and the range of the reference relative to
/// the start of the specialization.
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

let referenceReason (name: string) (range: _ QsNullable) (codeFile: string) diagnostic =
    let warningCode =
        match diagnostic.Diagnostic with
        | Error ErrorCode.UnsupportedResultComparison -> Some WarningCode.UnsupportedResultComparison
        | Error ErrorCode.ResultComparisonNotInOperationIf -> Some WarningCode.ResultComparisonNotInOperationIf
        | Error ErrorCode.ReturnInResultConditionedBlock -> Some WarningCode.ReturnInResultConditionedBlock
        | Error ErrorCode.SetInResultConditionedBlock -> Some WarningCode.SetInResultConditionedBlock
        | Error ErrorCode.UnsupportedCallableCapability -> Some WarningCode.UnsupportedCallableCapability
        | _ -> None

    let args =
        seq {
            name
            codeFile
            string (diagnostic.Range.Start.Line + 1)
            string (diagnostic.Range.Start.Column + 1)
            yield! diagnostic.Arguments
        }

    Option.map (fun code -> QsCompilerDiagnostic.Warning(code, args) (range.ValueOr Range.Zero)) warningCode

type CallKind =
    | External of RuntimeCapability
    | Recursive

type CallPattern =
    {
        Kind: CallKind
        Name: QsQualifiedName
        Range: Range QsNullable
    }

    interface IPattern with
        member pattern.Capability =
            match pattern.Kind with
            | External capability -> capability
            | Recursive -> RuntimeCapability.Base // TODO

        member pattern.Diagnose target =
            match pattern.Kind with
            | External capability ->
                if target.Capability.Implies capability then
                    None
                else
                    let args = [ pattern.Name.Name; string capability; target.Architecture ]
                    let range = pattern.Range.ValueOr Range.Zero
                    QsCompilerDiagnostic.Error(ErrorCode.UnsupportedCallableCapability, args) range |> Some
            | Recursive -> None // TODO

        member pattern.Explain(target, nsManager, graph) =
            let analyze env action =
                CallPattern.AnalyzeAllShallow(nsManager, graph, CallGraphNode pattern.Name, env, action)

            match nsManager.TryGetCallable pattern.Name ("", "") with
            | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
                let env = { CallableKind = callable.Kind }

                nsManager.ImportedSpecializations pattern.Name
                |> Seq.collect (fun (_, impl) ->
                    analyze env (fun t -> t.Namespaces.OnSpecializationImplementation impl |> ignore)
                    |> Seq.choose (fun p -> p.Diagnose target)
                    |> Seq.choose (referenceReason pattern.Name.Name pattern.Range callable.Source.CodeFile))
            | _ -> Seq.empty

    static member AnalyzeShallow(nsManager: NamespaceManager, graph: CallGraph, node: CallGraphNode, action) =
        let dependencies = graph.GetDirectDependencies node

        let codeFile, offset =
            match nsManager.TryGetCallable node.CallableName ("", "") with
            | Found ({ Position = DeclarationHeader.Defined p } as callable) -> callable.Source.CodeFile, p
            | _ -> failwith "Callable not found."

        seq {
            for name, range in globalReferences action do
                match nsManager.TryGetCallable name (node.CallableName.Namespace, codeFile) with
                | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
                    match SymbolResolution.TryGetRequiredCapability callable.Attributes with
                    | Value capability ->
                        {
                            Kind = External capability
                            Name = name
                            Range = range
                        }
                    | Null -> ()
                | _ -> ()

            for cycle in graph.GetCallCycles() |> Seq.filter (Seq.contains node) do
                for node in Seq.filter dependencies.Contains cycle do
                    for edge in dependencies[node] do
                        {
                            Kind = Recursive
                            Name = node.CallableName
                            Range = offset + edge.ReferenceRange |> Value
                        }
        }

    static member AnalyzeSyntax env action =
        Seq.collect
            ((|>) action)
            [
                ResultAnalyzer.analyze env
                StatementAnalyzer.analyze env
                TypeAnalyzer.analyze env
                ArrayAnalyzer.analyze env
            ]

    static member AnalyzeAllShallow(nsManager, graph, node: CallGraphNode, env, action) =
        Seq.append
            (CallPattern.AnalyzeSyntax env action)
            (CallPattern.AnalyzeShallow(nsManager, graph, node, action) |> Seq.map (fun p -> p :> IPattern))

let analyzeSyntax = CallPattern.AnalyzeSyntax

let analyzeAllShallow nsManager graph node env action =
    CallPattern.AnalyzeAllShallow(nsManager, graph, node, env, action)
