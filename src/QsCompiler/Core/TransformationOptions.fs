// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


/// Used to configure the behavior of the default implementations for transformations.
type TransformationOptions = {
    /// Disables the transformation at the transformation root, 
    /// meaning the transformation won't recur into leaf nodes or subnodes. 
    Disable : bool
    /// Indicates that the transformation is used to walk the syntax tree, 
    /// but does not modify any of the nodes. 
    /// If set to true, the nodes will hence not be rebuilt during the transformation. 
    /// Setting this to true constitutes a promise that the return value of all methods will be ignored. 
    DisableRebuild : bool
}
    with 
    
    /// Default transformation setting. 
    /// The transformation will recur into leaf and subnodes, 
    /// and all nodes will be rebuilt upon transformation.
    static member Default = {
        Disable = false 
        DisableRebuild = false
    }

    /// Disables the transformation at the transformation root, 
    /// meaning the transformation won't recur into leaf nodes or subnodes. 
    static member Disabled = {
        Disable = true
        DisableRebuild = false
    }


/// Tools for adapting the default implementations for transformations 
/// based on the specified options. 
module internal Utils = 
    type internal INode =
        abstract member BuildOr<'a, 'b> : 'b -> 'a -> ('a -> 'b) -> 'b

    let Fold = { new INode with member __.BuildOr _ arg builder = builder arg}
    let Walk = { new INode with member __.BuildOr original _ _ = original}

