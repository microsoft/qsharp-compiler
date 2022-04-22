// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement

[<Sealed>]
type internal CallPattern =
    interface IPattern

module internal CallPattern =
    val explain:
        nsManager: NamespaceManager ->
        graph: CallGraph ->
        target: Target ->
        pattern: CallPattern ->
            QsCompilerDiagnostic seq

module internal CallAnalyzer =
    val analyzeSyntax: Analyzer

    val analyzeAllShallow:
        nsManager: NamespaceManager ->
        graph: CallGraph ->
        node: CallGraphNode ->
        env: AnalyzerEnvironment ->
        action: AnalyzerAction ->
            IPattern seq * CallPattern seq
