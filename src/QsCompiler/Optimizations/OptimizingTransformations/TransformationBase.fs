// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A transformation base for optimizing syntax tree transformations.
/// It provides a function called `checkChanged` which returns true, if
/// the transformation leads to a change in any of the namespaces' syntax
/// tree, except for changes in the namespaces' documentation string.
type OptimizingTransformation() =
    inherit SyntaxTreeTransformation()

    let mutable changed = false

    /// Returns whether the syntax tree has been modified since this function was last called
    member internal __.checkChanged() =
        let x = changed
        changed <- false
        x

    /// Checks whether the syntax tree changed at all
    override __.Transform x =
        let newX = base.Transform x
        if (x.Elements, x.Name) <> (newX.Elements, newX.Name) then changed <- true
        newX


