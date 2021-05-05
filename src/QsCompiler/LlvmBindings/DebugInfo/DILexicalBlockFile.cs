// -----------------------------------------------------------------------
// <copyright file="DILexicalBlockFile.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Ubiquity.NET.Llvm.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a file lexical block</summary>
    /// <seealso href="xref:llvm_langref#dilexicalblockfile">LLVM DILexicalBlockBase</seealso>
    public class DILexicalBlockFile
        : DILexicalBlockBase
    {
        /* TODO: non-operand property
        unsigned Discriminator { get; }
        */

        internal DILexicalBlockFile( LLVMMetadataRef handle )
            : base( handle )
        {
        }
    }
}
