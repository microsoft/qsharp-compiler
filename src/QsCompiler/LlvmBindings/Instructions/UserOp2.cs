﻿// -----------------------------------------------------------------------
// <copyright file="UserOp2.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Custom operator that can be used in LLVM transform passes but should be removed before target instruction selection.</summary>
    public class UserOp2 : Instruction
    {
        internal UserOp2(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
