// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.Transformations.Core

type IPattern =
    abstract Capability: inOperation: bool -> RuntimeCapability
    abstract Diagnostic: context: ScopeContext -> QsCompilerDiagnostic option

type Analyzer = (SyntaxTreeTransformation -> unit) -> IPattern seq
