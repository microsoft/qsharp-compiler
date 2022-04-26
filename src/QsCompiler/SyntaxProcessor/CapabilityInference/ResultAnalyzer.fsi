// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ResultAnalyzer

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

val analyzer: callableKind: QsCallableKind -> Analyzer<SyntaxTreeTransformation -> unit, unit>
