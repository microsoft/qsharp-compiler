// -----------------------------------------------------------------------
// <copyright file="DILexicalBlock.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a Lexical block</summary>
    /// <seealso href="xref:llvm_langref#dilexicalblock">LLVM DILexicalBlock</seealso>
    public class DILexicalBlock
        : DILexicalBlockBase
    {
        internal DILexicalBlock(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
