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
    Analyzer.concat [ ResultAnalyzer.analyzer callableKind
                      StatementAnalyzer.analyzer
                      TypeAnalyzer.analyzer
                      ConstAnalyzer.analyzer ]

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
            analyzer callable.Kind (fun t -> t.Namespaces.OnSpecializationImplementation impl |> ignore)
            |> Seq.choose (fun p -> p.Diagnose target)
            |> Seq.choose (referenceReasons call.Name.Name call.Range callable.Source.CodeFile))
    | _ -> Seq.empty

[<CompiledName "Diagnose">]
let diagnose target nsManager graph (callable: QsCallable) =
    let patterns =
        syntaxAnalyzer callable.Kind (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)

    let callPatterns = CallGraphNode callable.FullName |> CallAnalyzer.shallow nsManager graph

    Seq.append
        (Seq.choose (fun p -> p.Diagnose target) patterns)
        (Seq.collect
            (fun p -> Seq.append (p.Diagnose target |> Option.toList) (explainCall nsManager graph target p.Properties))
            callPatterns)

let capabilityAttribute (capability: RuntimeCapability) =
    let args =
        AttributeUtils.StringArguments(
            string capability.ResultOpacity,
            string capability.Classical,
            "Inferred automatically by the compiler."
        )

    AttributeUtils.BuildAttribute(BuiltIn.RequiresCapability.FullName, args)

[<CompiledName "Infer">]
let infer compilation =
    let transformation = SyntaxTreeTransformation()
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = CallGraph compilation

    let analyzer =
        CallAnalyzer.deep callables graph (fun callable ->
            syntaxAnalyzer callable.Kind (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore))

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability =
                    SymbolResolution.TryGetRequiredCapability callable.Attributes |> QsNullable.isNull

                if isMissingCapability && QsNullable.isNull callable.Source.AssemblyFile then
                    analyzer callable |> Pattern.max |> capabilityAttribute |> callable.AddAttribute
                else
                    callable
        }

    transformation.OnCompilation compilation
