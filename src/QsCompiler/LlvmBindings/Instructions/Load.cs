// -----------------------------------------------------------------------
// <copyright file="Load.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to read from memory.</summary>
    /// <seealso href="xref:llvm_langref#load-instruction">LLVM load Instruction</seealso>
    public class Load
        : UnaryInstruction
    {
        /// <summary>Gets or sets a value indicating whether this load is volatile.</summary>
        public unsafe bool IsVolatile
        {
            get => this.ValueHandle.Volatile;
            set => LLVM.SetVolatile(this.ValueHandle, value ? 1 : 0);
        }

        internal Load(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
