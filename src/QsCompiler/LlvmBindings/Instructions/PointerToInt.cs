// -----------------------------------------------------------------------
// <copyright file="PointerToInt.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction to cast a pointer to an integer value.</summary>
    public class PointerToInt
        : Cast
    {
        internal PointerToInt(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
