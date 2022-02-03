// -----------------------------------------------------------------------
// <copyright file="ConstantAsMetadata.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;
using LlvmBindings.Values;

namespace LlvmBindings
{
    /// <summary>Constant <see cref="Value"/> as metadata</summary>
    public class ConstantAsMetadata
        : ValueAsMetadata
    {
        internal ConstantAsMetadata(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
