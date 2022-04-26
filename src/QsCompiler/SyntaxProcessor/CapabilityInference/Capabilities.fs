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
                      ArrayAnalyzer.analyzer ]

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

let explainCall (nsManager: NamespaceManager) graph target (pattern: CallPattern) =
    let node = CallGraphNode pattern.Name

    let analyzer callableKind action =
        Seq.append
            (syntaxAnalyzer callableKind action)
            (CallAnalyzer.shallow nsManager graph node |> Seq.map (fun p -> upcast p))

    match nsManager.TryGetCallable pattern.Name ("", "") with
    | Found callable when QsNullable.isValue callable.Source.AssemblyFile ->
        nsManager.ImportedSpecializations pattern.Name
        |> Seq.collect (fun (_, impl) ->
            analyzer callable.Kind (fun t -> t.Namespaces.OnSpecializationImplementation impl |> ignore)
            |> Seq.choose (fun p -> p.Diagnose target)
            |> Seq.choose (referenceReasons pattern.Name.Name pattern.Range callable.Source.CodeFile))
    | _ -> Seq.empty

[<CompiledName "Diagnose">]
let diagnose target nsManager graph (callable: QsCallable) =
    let patterns =
        syntaxAnalyzer callable.Kind (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)

    let callPatterns = CallGraphNode callable.FullName |> CallAnalyzer.shallow nsManager graph

    Seq.append
        (patterns |> Seq.choose (fun p -> p.Diagnose target))
        (callPatterns
         |> Seq.collect (fun p ->
             Seq.append ((p :> IPattern).Diagnose target |> Option.toList) (explainCall nsManager graph target p)))

let capabilityAttribute capability =
    let args = AttributeUtils.StringArguments(string capability, "Inferred automatically by the compiler.")
    AttributeUtils.BuildAttribute(BuiltIn.RequiresCapability.FullName, args)

[<CompiledName "InferAttributes">]
let inferAttributes compilation =
    let transformation = SyntaxTreeTransformation()
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = CallGraph compilation

    let analyzer =
        fun callable -> syntaxAnalyzer callable.Kind (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)
        |> CallAnalyzer.deep callables graph

    let callableCapability =
        analyzer
        >> Seq.map (fun p -> p.Capability)
        >> Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability =
                    SymbolResolution.TryGetRequiredCapability callable.Attributes |> QsNullable.isNull

                if isMissingCapability && QsNullable.isNull callable.Source.AssemblyFile then
                    callableCapability callable |> capabilityAttribute |> callable.AddAttribute
                else
                    callable
        }

    transformation.OnCompilation compilation
