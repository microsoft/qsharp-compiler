// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree

/// Returns all capability diagnostics for the callable.
[<CompiledName "Diagnose">]
val diagnose:
    target: Target ->
    nsManager: NamespaceManager ->
    graph: CallGraph ->
    callable: QsCallable ->
        QsCompilerDiagnostic seq

/// <summary>
/// Adds the <c>RequiresCapability</c> attribute to each callable in the compilation that is defined in a source file
/// (not a reference), representing its inferred required runtime capability.
/// </summary>
[<CompiledName "Infer">]
val infer: compilation: QsCompilation -> QsCompilation
