// -----------------------------------------------------------------------
// <copyright file="IntCmp.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Instruction to perform an integer compare.</summary>
    /// <seealso href="xref:llvm_langref#intcmp-instruction">LLVM intcmp Instruction</seealso>
    public class IntCmp
        : Cmp
    {
        internal IntCmp(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
