// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree

type internal Call =
    { Name: QsQualifiedName
      Range: Range QsNullable }

module internal CallAnalyzer =
    val shallow: nsManager: NamespaceManager -> graph: CallGraph -> Analyzer<CallGraphNode, Call>

    val deep:
        callables: ImmutableDictionary<QsQualifiedName, QsCallable> ->
        graph: CallGraph ->
        syntaxAnalyzer: Analyzer<QsCallable, unit> ->
            Analyzer<QsCallable, unit>
