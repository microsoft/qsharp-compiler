// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.Core

type LocationTrackingTransformation(options) =
    inherit SyntaxTreeTransformation(options)

    let mutable offset = Null

    member this.Offset = offset

    override this.OnRelativeLocation location =
        offset <- location |> QsNullable<_>.Map (fun l -> l.Offset)
        location
