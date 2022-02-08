// -----------------------------------------------------------------------
// <copyright file="BinaryOperator.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Base class for a binary operator.</summary>
    public class BinaryOperator
        : Instruction
    {
        internal BinaryOperator(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
