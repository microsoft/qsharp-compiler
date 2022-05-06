// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ConstAnalyzer

open Microsoft.Quantum.QsCompiler.Transformations.Core

val analyzer: Analyzer<SyntaxTreeTransformation -> unit, unit>
