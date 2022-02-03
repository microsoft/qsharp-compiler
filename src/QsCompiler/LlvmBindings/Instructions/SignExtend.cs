// -----------------------------------------------------------------------
// <copyright file="SignExtend.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Sign extension instruction.</summary>
    public class SignExtend
        : Cast
    {
        internal SignExtend(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
