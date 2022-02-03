// -----------------------------------------------------------------------
// <copyright file="GetElementPtr.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Instruction to compute the address of a sub element of an aggregate data type.</summary>
    /// <seealso href="xref:llvm_langref#getelementptr-instruction">LLVM getelementptr Instruction</seealso>
    public class GetElementPtr
        : Instruction
    {
        internal GetElementPtr(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
