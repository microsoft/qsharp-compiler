// -----------------------------------------------------------------------
// <copyright file="UIToFP.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Instruction to cast an unsigned integer to a float.</summary>
    public class UIToFP
        : Cast
    {
        internal UIToFP(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
