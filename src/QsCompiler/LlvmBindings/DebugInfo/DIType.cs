// -----------------------------------------------------------------------
// <copyright file="DIType.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Base class for Debug info types</summary>
    /// <seealso href="xref:llvm_langref#ditype">LLVM DIType</seealso>
    public class DIType
        : DIScope
    {
        /// <summary>Gets the containing scope for the type</summary>
        public override DIScope? Scope => this.MetadataHandle == default ? null : GetOperand<DIScope>( 1 );

        /// <summary>Gets the name of the type</summary>
        public override string Name => this.MetadataHandle == default ? string.Empty : GetOperand<MDString>( 2 )?.ToString() ?? string.Empty;

        /// <summary>Gets the source line for the type</summary>
        public UInt32 Line => this.MetadataHandle == default ? 0 : this.MetadataHandle.DITypeGetLine();

        /// <summary>Gets the size of the type in bits</summary>
        public UInt64 BitSize => this.MetadataHandle == default ? 0 : this.MetadataHandle.DITypeGetSizeInBits();

        /// <summary>Gets the alignment of the type in bits</summary>
        public UInt64 BitAlignment => this.MetadataHandle == default ? 0 : this.MetadataHandle.DITypeGetAlignInBits();

        /// <summary>Gets the offset of the type in bits</summary>
        public UInt64 BitOffset => this.MetadataHandle == default ? 0 : this.MetadataHandle.DITypeGetOffsetInBits();

        /// <summary>Gets the flags that describe the behaviors for</summary>
        public DebugInfoFlags DebugInfoFlags => this.MetadataHandle == default ? 0 : (DebugInfoFlags)this.MetadataHandle.DITypeGetFlags();

        internal DIType( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
