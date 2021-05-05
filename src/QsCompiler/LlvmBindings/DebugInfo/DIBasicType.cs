// -----------------------------------------------------------------------
// <copyright file="DIBasicType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a basic type</summary>
    /// <seealso cref="Ubiquity.NET.Llvm.DebugInfo.DebugInfoBuilder.CreateBasicType(string, ulong, DiTypeKind, DebugInfoFlags)"/>
    /// <seealso href="xref:llvm_langref#dibasictype">LLVM DIBasicType</seealso>
    public class DIBasicType
        : DIType
    {
        /// <summary>Gets the encoding for the type</summary>
        public DiTypeKind Encoding => ( DiTypeKind )LibLLVMDIBasicTypeGetEncoding( MetadataHandle );

        /// <summary>Initializes a new instance of the <see cref="DIBasicType"/> class.</summary>
        /// <param name="handle"><see cref="LLVMMetadataRef"/> for a  DIBasicType to wrap</param>
        internal DIBasicType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
