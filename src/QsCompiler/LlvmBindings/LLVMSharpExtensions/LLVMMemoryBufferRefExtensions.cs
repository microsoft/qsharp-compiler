// <copyright file="LLVMMemoryBufferRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMMemoryBufferRef class.</summary>
    public static unsafe class LLVMMemoryBufferRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.GetErrorMessage.</summary>
        public static void Close(this LLVMMemoryBufferRef self)
        {
            LLVM.DisposeMemoryBuffer(self);
            self.Handle = default;
        }
    }
}
