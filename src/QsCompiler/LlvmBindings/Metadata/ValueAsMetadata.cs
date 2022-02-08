// -----------------------------------------------------------------------
// <copyright file="ValueAsMetadata.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings
{
    /// <summary>Used to wrap an <see cref="Values.Value"/> in the Metadata hierarchy</summary>
    public class ValueAsMetadata
        : LlvmMetadata
    {
        private protected ValueAsMetadata(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
