// -----------------------------------------------------------------------
// <copyright file="DIMacro.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Pre-Processor macro</summary>
    public class DIMacro
        : DIMacroNode
    {
        /// <summary>Gets the name of the macro</summary>
        public string Name => this.GetOperandString(0);

        /// <summary>Gets the value of the property</summary>
        public string Value => this.GetOperandString(1);

        internal DIMacro(LLVMMetadataRef handle)
            : base(handle)
        {
        }
    }
}
