// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Transformations.Core


/// Used to configure the behavior of the default implementations for transformations.
type TransformationOptions =
    internal
        {
            /// If set to false, disables the transformation at the transformation root,
            /// meaning the transformation won't recur into leaf nodes or subnodes.
            Enable: bool
            /// Indicates whether the transformation modifies any of the nodes.
            /// If set to true, the nodes will be rebuilt during the transformation.
            /// Setting this to false constitutes a promise that the return value of all methods will be ignored.
            Rebuild: bool
        }

        /// Default transformation setting.
        /// The transformation will recur into leaf and subnodes,
        /// and all nodes will be rebuilt upon transformation.
        static member Default = { Enable = true; Rebuild = true }

        /// Disables the transformation at the transformation root,
        /// meaning the transformation won't recur into leaf nodes or subnodes.
        static member Disabled = { Enable = false; Rebuild = true }

        /// Indicates that the transformation is used to walk the syntax tree, but does not modify any of the nodes.
        /// All nodes will be traversed recursively, but the nodes will not be rebuilt.
        /// Setting this option constitutes a promise that the return value of all methods will be ignored.
        static member NoRebuild = { Enable = true; Rebuild = false }


/// Tools for adapting the default implementations for transformations
/// based on the specified options.
module internal Utils =
    type internal INode =
        abstract BuildOr<'a, 'b> : 'b -> 'a -> ('a -> 'b) -> 'b

    let Fold =
        { new INode with
            member __.BuildOr _ arg builder = builder arg
        }

    let Walk =
        { new INode with
            member __.BuildOr original _ _ = original
        }
