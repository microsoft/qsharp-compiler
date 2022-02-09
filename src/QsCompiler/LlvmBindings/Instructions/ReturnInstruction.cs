// -----------------------------------------------------------------------
// <copyright file="ReturnInstruction.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Return instruction.</summary>
    public class ReturnInstruction
        : Terminator
    {
        internal ReturnInstruction(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
