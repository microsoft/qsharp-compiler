// -----------------------------------------------------------------------
// <copyright file="MemMove.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LlvmBindings.Interop;

namespace LlvmBindings.Instructions
{
    /// <summary>Intrinsic call to target optimized memmove.</summary>
    public class MemMove
        : MemIntrinsic
    {
        internal MemMove(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
