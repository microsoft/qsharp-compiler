// -----------------------------------------------------------------------
// <copyright file="InsertElement.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to insert an element into a vector type.</summary>
    /// <seealso href="xref:llvm_langref#insertelement-instruction">LLVM insertelement Instruction</seealso>
    public class InsertElement
        : Instruction
    {
        internal InsertElement(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
