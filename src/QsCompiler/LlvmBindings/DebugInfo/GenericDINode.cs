// -----------------------------------------------------------------------
// <copyright file="GenericDINode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Generic tagged DWARF-like Metadata node</summary>
    public class GenericDINode
        : DINode
    {
        /// <summary>Gets the header for this node</summary>
        /// <remarks>
        /// The header is a, possibly empty, null separated string
        /// header that contains arbitrary fields.
        /// </remarks>
        public string Header => GetOperandString( 0 );

        internal GenericDINode( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
