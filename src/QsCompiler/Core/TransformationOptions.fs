// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


/// Used to configure the behavior of the default implementations for transformations.
type TransformationOptions = {
    Disable : bool
    DisableRebuild : bool
}
    with 
    static member Default = {
        Disable = false 
        DisableRebuild = false
    }


/// Tools for adapting the default implementations for transformations 
/// based on the specified options. 
module internal Utils = 
    type internal INode =
        abstract member Build<'a, 'b> : ('a -> 'b) -> 'a -> 'b -> 'b

    let Fold = { new INode with member __.Build builder arg _ = builder arg}
    let Walk = { new INode with member __.Build _ _ original = original}

