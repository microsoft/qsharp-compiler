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

    let analyzer (nsManager: NamespaceManager) (graph: CallGraph) node =
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
