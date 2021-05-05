// -----------------------------------------------------------------------
// <copyright file="DIMacroNode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Base class for macro related nodes in the debug hierarchy</summary>
    public class DIMacroNode
        : MDNode
    {
        internal DIMacroNode( LLVMMetadataRef handle )
           : base( handle )
        {
        }
    }
}
