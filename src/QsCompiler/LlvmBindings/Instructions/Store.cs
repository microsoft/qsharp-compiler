// -----------------------------------------------------------------------
// <copyright file="Store.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to store a value to memory.</summary>
    public class Store
        : Instruction
    {
        /// <summary>Gets or sets a value indicating whether the store is volatile.</summary>
        public unsafe bool IsVolatile
        {
            get => this.ValueHandle.Volatile;
            set => LLVM.SetVolatile(this.ValueHandle, value ? 1 : 0);
        }

        internal Store(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
