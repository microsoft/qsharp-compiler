// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

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

module CallPattern =
    let analyzeSyntax callableKind action =
        Seq.collect
            ((|>) action)
            [
                ResultAnalyzer.analyze callableKind
                StatementAnalyzer.analyze ()
                TypeAnalyzer.analyze ()
                ArrayAnalyzer.analyze ()
            ]

    /// Returns a list of the names of global callables referenced in the scope, and the range of the reference relative
    /// to the start of the specialization.
    let globalReferencesFromSyntax action =
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

    let analyzeShallow (nsManager: NamespaceManager) (graph: CallGraph) node =
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
                    globalReferencesFromSyntax (fun t -> t.Namespaces.OnSpecializationImplementation spec |> ignore))
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

    let analyzeAllShallow nsManager graph node callableKind action =
        analyzeSyntax callableKind action, analyzeShallow nsManager graph node

    let referenceReasons (name: string) (range: _ QsNullable) (codeFile: string) diagnostic =
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

    let explain nsManager graph target pattern =
        let analyze env action =
            let patterns, callPatterns = analyzeAllShallow nsManager graph (CallGraphNode pattern.Name) env action
            Seq.append patterns (Seq.map (fun p -> p :> IPattern) callPatterns)

        match nsManager.TryGetCallable pattern.Name ("", "") with
        | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
            nsManager.ImportedSpecializations pattern.Name
            |> Seq.collect (fun (_, impl) ->
                analyze callable.Kind (fun t -> t.Namespaces.OnSpecializationImplementation impl |> ignore)
                |> Seq.choose (fun p -> p.Diagnose target)
                |> Seq.choose (referenceReasons pattern.Name.Name pattern.Range callable.Source.CodeFile))
        | _ -> Seq.empty

module CallAnalyzer =
    let analyzeSyntax = CallPattern.analyzeSyntax

    let analyzeShallow (nsManager, graph) node =
        CallPattern.analyzeShallow nsManager graph node

    let analyzeAllShallow nsManager graph node callableKind action =
        CallPattern.analyzeAllShallow nsManager graph node callableKind action
