// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Target =
    { Capability: RuntimeCapability
      Architecture: string }

type internal IPattern =
    /// The required runtime capability of the pattern.
    abstract Capability: RuntimeCapability

    /// Returns a diagnostic for the pattern if the pattern's capability level exceeds the execution target's capability
    /// level.
    abstract Diagnose: target: Target -> QsCompilerDiagnostic option

type internal AnalyzerEnvironment = { CallableKind: QsCallableKind }

type internal AnalyzerAction = SyntaxTreeTransformation -> unit

type internal Analyzer = AnalyzerEnvironment -> AnalyzerAction -> IPattern seq

/// Tracks the most recently seen statement location.
type internal LocationTrackingTransformation =
    inherit SyntaxTreeTransformation

    /// Creates a new location tracking transformation.
    new: options: TransformationOptions -> LocationTrackingTransformation

    /// The offset of the most recently seen statement location.
    member Offset: Position QsNullable
