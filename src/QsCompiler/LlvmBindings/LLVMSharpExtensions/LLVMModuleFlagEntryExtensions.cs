// <copyright file="LLVMMemoryBufferRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMDIBuilderRef class.</summary>
    public static unsafe class LLVMModuleFlagEntryExtensions
    {
        public static void Dispose(this LLVMModuleFlagEntry entries)
        {
            LLVM.DisposeModuleFlagsMetadata(entries);
        }

        public static LLVMModuleFlagBehavior GetFlagBehavior(this LLVMModuleFlagEntry entries, uint index)
        {
            return LLVM.ModuleFlagEntriesGetFlagBehavior(entries, index);
        }

        public static string GetKey(this LLVMModuleFlagEntry entries, uint index)
        {
            UIntPtr len;
            sbyte* pStr = LLVM.ModuleFlagEntriesGetKey(entries, index, &len);

            if (pStr == default)
            {
                return string.Empty;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static LLVMMetadataRef GetMetadata(this LLVMModuleFlagEntry entries, uint index)
        {
            return LLVM.ModuleFlagEntriesGetMetadata(entries, index);
        }
    }
}
