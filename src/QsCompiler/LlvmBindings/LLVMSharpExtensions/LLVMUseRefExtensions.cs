// <copyright file="LLVMUseRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMUseRef.</summary>
    public static unsafe class LLVMUseRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.GetNextUse.</summary>
        public static LLVMUseRef Next(this LLVMUseRef self) => (self.Handle != default) ? LLVM.GetNextUse(self) : default;

        /// <summary>Convenience wrapper for LLVM.GetUser.</summary>
        public static LLVMValueRef GetUser(this LLVMUseRef self) => (self.Handle != default) ? LLVM.GetUser(self) : default;

        /// <summary>Convenience wrapper for LLVM.GetUsedValue.</summary>
        public static LLVMValueRef GetUsedValue(this LLVMUseRef self) => (self.Handle != default) ? LLVM.GetUsedValue(self) : default;
    }
}
