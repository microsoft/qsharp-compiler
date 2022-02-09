// <copyright file="LLVMModuleFlagEntryExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMModuleFlagEntry"/>.</summary>
    public static unsafe class LLVMModuleFlagEntryExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.DisposeModuleFlagsMetadata"/>.</summary>
        public static void Dispose(this LLVMModuleFlagEntry entries)
        {
            LLVM.DisposeModuleFlagsMetadata(entries);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.ModuleFlagEntriesGetFlagBehavior"/>.</summary>
        public static LLVMModuleFlagBehavior GetFlagBehavior(this LLVMModuleFlagEntry entries, uint index)
        {
            return LLVM.ModuleFlagEntriesGetFlagBehavior(entries, index);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.ModuleFlagEntriesGetKey"/>.</summary>
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

        /// <summary>Convenience wrapper for <see cref="LLVM.ModuleFlagEntriesGetMetadata"/>.</summary>
        public static LLVMMetadataRef GetMetadata(this LLVMModuleFlagEntry entries, uint index)
        {
            return LLVM.ModuleFlagEntriesGetMetadata(entries, index);
        }
    }
}
