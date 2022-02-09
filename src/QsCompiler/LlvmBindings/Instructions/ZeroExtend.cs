// -----------------------------------------------------------------------
// <copyright file="ZeroExtend.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to zero extend a value.</summary>
    public class ZeroExtend
        : Cast
    {
        internal ZeroExtend(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
