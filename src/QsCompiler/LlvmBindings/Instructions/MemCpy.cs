// -----------------------------------------------------------------------
// <copyright file="MemCpy.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction for the LLVM intrinsic llvm.memcpy instruction.</summary>
    public class MemCpy
        : MemIntrinsic
    {
        internal MemCpy(LLVMValueRef handle)
            : base(handle)
        {
        }
    }
}
