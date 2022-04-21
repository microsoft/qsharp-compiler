// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing

type internal IPattern =
    /// Returns the required runtime capability of the pattern, given whether it occurs in an operation.
    abstract Capability: inOperation: bool -> RuntimeCapability

    /// Returns a diagnostic for the pattern if the pattern's capability level exceeds the execution target's capability
    /// level.
    abstract Diagnose: context: ScopeContext -> QsCompilerDiagnostic option

    abstract Explain: context: ScopeContext -> QsCompilerDiagnostic seq
