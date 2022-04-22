// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

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
    val analyzeSyntax: Analyzer<QsCallableKind, SyntaxTreeTransformation -> unit, IPattern>

    val analyzeShallow: Analyzer<NamespaceManager * CallGraph, CallGraphNode, CallPattern>

    val analyzeAllShallow:
        nsManager: NamespaceManager ->
        graph: CallGraph ->
        node: CallGraphNode ->
        callableKind: QsCallableKind ->
        action: (SyntaxTreeTransformation -> unit) ->
            IPattern seq * CallPattern seq
