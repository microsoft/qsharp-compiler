// -----------------------------------------------------------------------
// <copyright file="ValueAsMetadata.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Used to wrap an <see cref="Ubiquity.NET.Llvm.Values.Value"/> in the Metadata hierarchy</summary>
    public class ValueAsMetadata
        : LlvmMetadata
    {
        private protected ValueAsMetadata( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
