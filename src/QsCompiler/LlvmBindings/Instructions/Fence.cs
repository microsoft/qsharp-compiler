// -----------------------------------------------------------------------
// <copyright file="Fence.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Synchronization instruction to introduce "happens-before" edges between operations.</summary>
    /// <seealso href="xref:llvm_langref#fence-instruction">LLVM fence Instruction</seealso>
    public class Fence
        : Instruction
    {
        internal Fence(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
