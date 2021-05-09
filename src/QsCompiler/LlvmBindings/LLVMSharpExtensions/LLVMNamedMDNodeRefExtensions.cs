// <copyright file="LLVMMemoryBufferRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMNamedMDNodeRef class.</summary>
    public static unsafe class LLVMNamedMDNodeRefExtensions
    {
        public static string Name(this LLVMNamedMDNodeRef self)
        {
            UIntPtr len;
            var pStr = LLVM.GetNamedMetadataName(self, &len);
            if (pStr == default)
            {
                return string.Empty;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }
    }
}
