// <copyright file="LLVMModuleRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
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
