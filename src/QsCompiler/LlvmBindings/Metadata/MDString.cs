// -----------------------------------------------------------------------
// <copyright file="MDString.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
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
        public override string ToString()
        {
            var context = ThreadContextCache.Get();
            var asValue = context.ContextHandle.MetadataAsValue(this.MetadataHandle);
            return asValue.GetMDString();
        }

        internal MDString(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
