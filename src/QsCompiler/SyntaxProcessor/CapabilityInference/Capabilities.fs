// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.Core

let syntaxAnalyzer callableKind =
    Analyzer.concat [ ConstAnalyzer.analyzer
                      FeatureAnalyzer.analyzer
                      ResultAnalyzer.analyzer callableKind
                      TypeAnalyzer.analyzer ]

// TODO: Remove this function as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
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

    Option.map (fun code -> QsCompilerDiagnostic.Warning (code, args) (range.ValueOr Range.Zero)) warningCode

// TODO: Remove this function as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
let explainCall (nsManager: NamespaceManager) graph target (call: Call) =
    let node = CallGraphNode call.Name

    let analyzer callableKind action =
        Seq.append
            (syntaxAnalyzer callableKind action)
            (CallAnalyzer.shallow nsManager graph node |> Seq.map Pattern.discard)

    match nsManager.TryGetCallable call.Name ("", "") with
    | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
        nsManager.ImportedSpecializations call.Name
        |> Seq.collect (fun (_, impl) ->
            analyzer (Some callable.Kind) (fun t -> t.Namespaces.OnSpecializationImplementation impl |> ignore)
            |> Seq.choose (fun p -> p.Diagnose target)
            |> Seq.choose (referenceReasons call.Name.Name call.Range callable.Source.CodeFile))
    | _ -> Seq.empty

// TODO: Remove this function as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
let diagnoseCall target nsManager graph pattern =
    match pattern.Diagnose target with
    | Some d -> Seq.append (Seq.singleton d) (explainCall nsManager graph target pattern.Properties)
    | None -> Seq.empty

[<CompiledName "Diagnose">]
let diagnose target nsManager graph (callable: QsCallable) =
    let patterns = syntaxAnalyzer None (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)
    let callPatterns = CallGraphNode callable.FullName |> CallAnalyzer.shallow nsManager graph
    let callDiagnostics = Seq.collect (diagnoseCall target nsManager graph) callPatterns
    Seq.append (Seq.choose (fun p -> p.Diagnose target) patterns) callDiagnostics

let capabilityAttribute (capability: TargetCapability) =
    let args =
        AttributeUtils.StringArguments(
            string capability.ResultOpacity,
            string capability.ClassicalCompute,
            "Inferred automatically by the compiler."
        )

    AttributeUtils.BuildAttribute(BuiltIn.RequiresCapability.FullName, args)

[<CompiledName "Infer">]
let infer compilation =
    let transformation = SyntaxTreeTransformation()
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = CallGraph compilation
    let syntaxAnalyzer = syntaxAnalyzer None

    let analyzer =
        CallAnalyzer.deep callables graph (fun callable ->
            syntaxAnalyzer (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore))

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability =
                    SymbolResolution.TryGetRequiredCapability callable.Attributes |> QsNullable.isNull

                if isMissingCapability && QsNullable.isNull callable.Source.AssemblyFile then
                    analyzer callable |> Pattern.concat |> capabilityAttribute |> callable.AddAttribute
                else
                    callable
        }

    transformation.OnCompilation compilation
