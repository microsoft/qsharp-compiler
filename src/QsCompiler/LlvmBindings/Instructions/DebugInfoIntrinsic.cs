﻿// -----------------------------------------------------------------------
// <copyright file="DebugInfoIntrinsic.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for debug information intrinsic functions in LLVM IR.</summary>
    public class DebugInfoIntrinsic
        : Intrinsic
    {
        internal DebugInfoIntrinsic(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
