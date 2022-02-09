// -----------------------------------------------------------------------
// <copyright file="DILocalVariable.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Debug information for a local variable</summary>
    /// <seealso href="xref:llvm_langref#dilocalvariable">LLVM DILocalVariable</seealso>
    public class DILocalVariable
        : DIVariable
    {
        /// <summary>Gets the local scope containing this variable</summary>
        public new DILocalScope Scope => (DILocalScope)base.Scope!;

        internal DILocalVariable(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
