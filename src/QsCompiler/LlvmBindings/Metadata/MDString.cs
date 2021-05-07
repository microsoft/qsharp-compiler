// -----------------------------------------------------------------------
// <copyright file="MDString.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Stores a string in Metadata</summary>
    public class MDString
        : LlvmMetadata
    {
        /// <summary>Gets the string from the metadata node</summary>
        /// <returns>String this node wraps</returns>
        public override string ToString( )
        {
            // TODO: might need to get context, and then metadataAsValue
            //var asValue = LLVM.MetadataAsValue
            var asValue = new LLVMValueRef(this.MetadataHandle.Handle);
            return asValue.GetMDString();
        }

        internal MDString( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
