// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// Tracks the most recently seen statement location.
type internal LocationTrackingTransformation =
    inherit SyntaxTreeTransformation

    /// Creates a new location tracking transformation.
    new: options: TransformationOptions -> LocationTrackingTransformation

    /// The offset of the most recently seen statement location.
    member Offset: Position QsNullable
