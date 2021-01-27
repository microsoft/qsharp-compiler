// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Experimental

open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core


/// A transformation base for optimizing syntax tree transformations.
/// It provides a function called `checkChanged` which returns true, if
/// the transformation leads to a change in any of the namespaces' syntax
/// tree, except for changes in the namespaces' documentation string.
type TransformationBase private (_private_) =
    inherit SyntaxTreeTransformation()

    member val Changed = false with get, set

    /// Returns whether the syntax tree has been modified since this function was last called
    member internal this.CheckChanged() =
        let res = this.Changed
        this.Changed <- false
        res

    new() as this =
        new TransformationBase("_private_")
        then this.Namespaces <- new NamespaceTransformationBase(this)

/// private helper class for OptimizingTransformation
and private NamespaceTransformationBase(parent: TransformationBase) =
    inherit NamespaceTransformation(parent)

    /// Checks whether the syntax tree changed at all
    override this.OnNamespace x =
        let newX = base.OnNamespace x

        if (x.Elements, x.Name) <> (newX.Elements, newX.Name)
        then parent.Changed <- true

        newX
