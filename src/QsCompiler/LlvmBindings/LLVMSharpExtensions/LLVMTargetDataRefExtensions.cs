// <copyright file="LLVMTargetDataRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMTargetDataRef"/>.</summary>
    public static unsafe class LLVMTargetDataRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.ByteOrder"/>.</summary>
        public static LLVMByteOrdering ByteOrder(this LLVMTargetDataRef self)
        {
            return LLVM.ByteOrder(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.PointerSize"/>.</summary>
        public static uint PointerSize(this LLVMTargetDataRef self)
        {
            return LLVM.PointerSize(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.PointerSizeForAS"/>.</summary>
        public static uint PointerSizeForAS(this LLVMTargetDataRef self, uint aS)
        {
            return LLVM.PointerSizeForAS(self, aS);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.CopyStringRepOfTargetData"/>.</summary>
        public static string CopyStringRepOfTargetData(this LLVMTargetDataRef self)
        {
            var pStr = LLVM.CopyStringRepOfTargetData(self);
            return new string(pStr);
        }
    }
}
