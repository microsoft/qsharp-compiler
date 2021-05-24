// <copyright file="LLVMValueRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMValueRef"/>.</summary>
    public static unsafe class LLVMValueRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.GetNextGlobalAlias"/>.</summary>
        public static LLVMValueRef NextGlobalAlias(this LLVMValueRef self) => (self.Handle != default) ? LLVM.GetNextGlobalAlias(self) : default;

        /// <summary>Convenience wrapper for <see cref="LLVM.GetNextGlobalIFunc"/>.</summary>
        public static LLVMValueRef NextGlobalIFunc(this LLVMValueRef self) => (self.Handle != default) ? LLVM.GetNextGlobalIFunc(self) : default;

        /// <summary>Convenience wrapper for <see cref="LLVM.GetMDString"/>.</summary>
        public static string GetMDString(this LLVMValueRef self)
        {
            var mdString = self.IsAMDString;
            if (mdString.Handle == default)
            {
                return string.Empty;
            }

            uint len;
            var pStr = LLVM.GetMDString(mdString, &len);
            if (pStr == default)
            {
                return string.Empty;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GlobalCopyAllMetadata"/>.</summary>
        public static (LLVMValueMetadataEntryRef MetadataRef, uint Count) GlobalCopyAllMetadata(this LLVMValueRef self)
        {
            UIntPtr count;
            LLVMValueMetadataEntryRef metadataRef = LLVM.GlobalCopyAllMetadata(self, &count);
            return (metadataRef, (uint)count);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetSubprogram"/>.</summary>
        public static LLVMMetadataRef GetSubprogram(this LLVMValueRef self)
        {
            return LLVM.GetSubprogram(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.SetSubprogram"/>.</summary>
        public static void SetSubprogram(this LLVMValueRef self, LLVMMetadataRef subprogram)
        {
            LLVM.SetSubprogram(self, subprogram);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.ValueAsMetadata"/>.</summary>
        public static LLVMMetadataRef ValueAsMetadata(this LLVMValueRef self)
        {
            return LLVM.ValueAsMetadata(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetMDNodeOperands"/>.</summary>
        public static LLVMValueRef[] GetMDNodeOperands(this LLVMValueRef self)
        {
            var dest = new LLVMValueRef[self.GetMDNodeNumOperands()];

            fixed (LLVMValueRef* pDest = dest)
            {
                LLVM.GetMDNodeOperands(self, (LLVMOpaqueValue**)pDest);
            }

            return dest;
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.GetMDNodeNumOperands"/>.</summary>
        public static uint GetMDNodeNumOperands(this LLVMValueRef self)
        {
            return LLVM.GetMDNodeNumOperands(self);
        }
    }
}
