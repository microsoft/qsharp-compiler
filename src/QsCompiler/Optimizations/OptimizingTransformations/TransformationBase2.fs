// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental


open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// A transformation base for optimizing syntax tree transformations.
/// It provides a function called `checkChanged` which returns true, if
/// the transformation leads to a change in any of the namespaces' syntax
/// tree, except for changes in the namespaces' documentation string.
type TransformationBase () =
    inherit MonoTransformation()

    member val Changed = false with get, set

    /// Returns whether the syntax tree has been modified since this function was last called
    member this.CheckChanged() =
        let res = this.Changed
        this.Changed <- false
        res

    /// Checks whether the syntax tree changed at all
    override this.OnNamespace x =
        let newX = base.OnNamespace x

        if (x.Elements, x.Name) <> (newX.Elements, newX.Name) then this.Changed <- true

        newX
