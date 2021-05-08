// <copyright file="LLVMModuleRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMTargetDataRef class.</summary>
    public static unsafe class LLVMTargetDataRefExtensions
    {
        public static LLVMByteOrdering ByteOrder(this LLVMTargetDataRef self)
        {
            return LLVM.ByteOrder(self);
        }

        public static uint PointerSize(this LLVMTargetDataRef self)
        {
            return LLVM.PointerSize(self);
        }

        public static uint PointerSizeForAS(this LLVMTargetDataRef self, uint AS)
        {
            return LLVM.PointerSizeForAS(self, AS);
        }

        public static string CopyStringRepOfTargetData(this LLVMTargetDataRef self)
        {
            var pStr = LLVM.CopyStringRepOfTargetData(self);
            return new string(pStr);
        }
    }
}
