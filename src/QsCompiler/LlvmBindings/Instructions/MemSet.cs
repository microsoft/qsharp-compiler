// -----------------------------------------------------------------------
// <copyright file="MemSet.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Instruction for the LLVM intrinsic memset function</summary>
    public class MemSet
        : MemIntrinsic
    {
        internal MemSet( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
