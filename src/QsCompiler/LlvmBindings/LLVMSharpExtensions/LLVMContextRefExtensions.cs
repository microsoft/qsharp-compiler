// <copyright file="LLVMContextRefExtensions.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMContextRef"/>.</summary>
    public static unsafe class LLVMContextRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVMContextRef.GetMDString"/>.</summary>
        public static LLVMValueRef GetMDString(this LLVMContextRef self, string str)
        {
            return self.GetMDString(str, (uint)str.Length);
        }
    }
}
