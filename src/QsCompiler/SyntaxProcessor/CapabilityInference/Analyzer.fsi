// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Target =
    { Capability: RuntimeCapability
      Architecture: string }

type internal 'props Pattern =
    { /// The required runtime capability of the pattern.
      Capability: RuntimeCapability
      /// A diagnostic for the pattern if the pattern's capability level exceeds the execution target's capability
      /// level.
      Diagnose: Target -> QsCompilerDiagnostic option
      /// Additional properties for the pattern.
      Properties: 'props }

module internal Pattern =
    val discard: 'props Pattern -> unit Pattern

type internal Analyzer<'subject, 'props> = 'subject -> 'props Pattern seq

module internal Analyzer =
    val concat: analyzers: Analyzer<'subject, 'props> seq -> Analyzer<'subject, 'props>

/// Tracks the most recently seen statement location.
type internal LocationTrackingTransformation =
    inherit SyntaxTreeTransformation

    /// Creates a new location tracking transformation.
    new: options: TransformationOptions -> LocationTrackingTransformation

    /// The offset of the most recently seen statement location.
    member Offset: Position QsNullable
