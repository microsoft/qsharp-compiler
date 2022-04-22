// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.CallAnalyzer

open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement

val analyzeSyntax: Analyzer

val analyzeAllShallow: nsManager: NamespaceManager -> graph: CallGraph -> node: CallGraphNode -> Analyzer
