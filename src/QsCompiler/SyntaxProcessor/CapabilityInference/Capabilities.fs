// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.Core

let syntaxAnalyzer callableKind =
    Analyzer.concat [ ConstAnalyzer.analyzer
                      FeatureAnalyzer.analyzer
                      ResultAnalyzer.analyzer callableKind
                      TypeAnalyzer.analyzer ]

[<CompiledName "Diagnose">]
let diagnose target nsManager graph (callable: QsCallable) =
    let patterns = syntaxAnalyzer None (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)
    let callPatterns = CallGraphNode callable.FullName |> CallAnalyzer.shallow nsManager graph
    let callDiagnostics = callPatterns |> Seq.choose (fun p -> p.Diagnose target)
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
