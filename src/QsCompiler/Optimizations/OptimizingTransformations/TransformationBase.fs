// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A transformation base for optimizing syntax tree transformations.
/// It provides a function called `checkChanged` which returns true, if
/// the transformation leads to a change in any of the namespaces' syntax
/// tree, except for changes in the namespaces' documentation string.
type OptimizingTransformation<'T> private (state : 'T, unsafe) =
    inherit QsSyntaxTreeTransformation<'T>(state)

    member val Changed = false with get, set

    /// Returns whether the syntax tree has been modified since this function was last called
    member internal this.CheckChanged() =
        let res = this.Changed
        this.Changed <- false
        res

    new (state : 'T) as this = 
        new OptimizingTransformation<_>(state, "unsafe") then
            this.Namespaces <- new OptimizingTransformationNamespaces<_>(this)

/// private helper class for OptimizingTransformation
and private OptimizingTransformationNamespaces<'T> (parent : OptimizingTransformation<'T>) = 
    inherit NamespaceTransformation<'T>(parent)

    /// Checks whether the syntax tree changed at all
    override this.Transform x =
        let newX = base.Transform x
        if (x.Elements, x.Name) <> (newX.Elements, newX.Name) then parent.Changed <- true
        newX


