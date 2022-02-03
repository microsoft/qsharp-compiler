// -----------------------------------------------------------------------
// <copyright file="BitCast.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>This class represents a no-op cast from one type to another.</summary>
    public class BitCast
        : Cast
    {
        internal BitCast(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
