// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// Returns all capability diagnostics for the scope. Ranges are relative to the start of the specialization.
[<CompiledName "Diagnose">]
val diagnose:
    target: Target ->
    nsManager: NamespaceManager ->
    graph: CallGraph ->
    callable: QsCallable ->
        QsCompilerDiagnostic seq

/// Infers the capability of all callables in the compilation and adds the corresponding attribute to each one.
[<CompiledName "Infer">]
val infer: compilation: QsCompilation -> QsCompilation
