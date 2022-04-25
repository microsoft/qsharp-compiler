// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree

[<Sealed>]
type internal CallPattern =
    interface IPattern

    member Name: QsQualifiedName

    member Range: Range QsNullable

module internal CallAnalyzer =
    val shallow: nsManager: NamespaceManager -> graph: CallGraph -> Analyzer<CallGraphNode, CallPattern>

    val deep:
        callables: ImmutableDictionary<QsQualifiedName, QsCallable> ->
        graph: CallGraph ->
        syntaxAnalyzer: Analyzer<QsCallable, IPattern> ->
            Analyzer<QsCallable, IPattern>

    val isLocal: source: Source -> bool
