// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing

type IPattern =
    abstract Capability: inOperation: bool -> RuntimeCapability
    abstract Diagnose: context: ScopeContext -> QsCompilerDiagnostic option
    abstract Explain: context: ScopeContext -> QsCompilerDiagnostic seq
