// -----------------------------------------------------------------------
// <copyright file="DIType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using Ubiquity.NET.Llvm.Interop;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Base class for Debug info types</summary>
    /// <seealso href="xref:llvm_langref#ditype">LLVM DIType</seealso>
    public class DIType
        : DIScope
    {
        /// <summary>Gets the containing scope for the type</summary>
        public override DIScope? Scope => MetadataHandle == default ? null : GetOperand<DIScope>( 1 );

        /// <summary>Gets the name of the type</summary>
        public override string Name => MetadataHandle == default ? string.Empty : GetOperand<MDString>( 2 )?.ToString() ?? string.Empty;

        /// <summary>Gets the source line for the type</summary>
        public UInt32 Line => MetadataHandle == default ? 0 : LLVMDITypeGetLine( MetadataHandle );

        /// <summary>Gets the size of the type in bits</summary>
        public UInt64 BitSize => MetadataHandle == default ? 0 : LLVMDITypeGetSizeInBits( MetadataHandle );

        /// <summary>Gets the alignment of the type in bits</summary>
        public UInt64 BitAlignment => MetadataHandle == default ? 0 : LLVMDITypeGetAlignInBits( MetadataHandle );

        /// <summary>Gets the offset of the type in bits</summary>
        public UInt64 BitOffset => MetadataHandle == default ? 0 : LLVMDITypeGetOffsetInBits( MetadataHandle );

        /// <summary>Gets the flags that describe the behaviors for</summary>
        public DebugInfoFlags DebugInfoFlags => MetadataHandle == default ? 0 : ( DebugInfoFlags )LLVMDITypeGetFlags( MetadataHandle );

        internal DIType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
