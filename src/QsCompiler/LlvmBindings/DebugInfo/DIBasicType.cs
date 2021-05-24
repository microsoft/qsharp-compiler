// -----------------------------------------------------------------------
// <copyright file="DIBasicType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a basic type</summary>
    /// <seealso cref="DebugInfoBuilder.CreateBasicType(string, ulong, DiTypeKind, DebugInfoFlags)"/>
    /// <seealso href="xref:llvm_langref#dibasictype">LLVM DIBasicType</seealso>
    public class DIBasicType
        : DIType
    {
        /// <summary>Initializes a new instance of the <see cref="DIBasicType"/> class.</summary>
        /// <param name="handle"><see cref="LLVMMetadataRef"/> for a  DIBasicType to wrap</param>
        internal DIBasicType(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
