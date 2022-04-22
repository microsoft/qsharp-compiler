// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.ArrayAnalyzer

open Microsoft.Quantum.QsCompiler.Transformations.Core

val analyze: Analyzer<unit, SyntaxTreeTransformation -> unit, IPattern>
