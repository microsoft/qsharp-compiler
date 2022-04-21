// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.CallAnalyzer

open Microsoft.Quantum.QsCompiler.SyntaxTree

val analyzeSyntax: Analyzer

val analyzeAllShallow: Analyzer

val analyzeScope: scope: QsScope -> analyzer: Analyzer -> IPattern seq
