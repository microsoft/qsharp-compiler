// -----------------------------------------------------------------------
// <copyright file="ExtractValue.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Instruction to extract the value of a member field from an aggregate value.</summary>
    /// <seealso href="xref:llvm_langref#extractvalue-instruction">LLVM extractvalue Instruction</seealso>
    public class ExtractValue
        : UnaryInstruction
    {
        internal ExtractValue(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
