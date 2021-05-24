// -----------------------------------------------------------------------
// <copyright file="DILexicalBlockFile.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a file lexical block</summary>
    /// <seealso href="xref:llvm_langref#dilexicalblockfile">LLVM DILexicalBlockBase</seealso>
    public class DILexicalBlockFile
        : DILexicalBlockBase
    {
        internal DILexicalBlockFile(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
