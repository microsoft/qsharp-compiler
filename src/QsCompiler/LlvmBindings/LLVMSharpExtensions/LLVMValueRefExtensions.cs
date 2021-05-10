// <copyright file="LLVMValueRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMValueRef class.</summary>
    public static unsafe class LLVMValueRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.GetNextGlobalAlias.</summary>
        public static LLVMValueRef NextGlobalAlias(this LLVMValueRef self) => (self.Handle != default) ? LLVM.GetNextGlobalAlias(self) : default;

        /// <summary>Convenience wrapper for LLVM.GetNextGlobalIFunc.</summary>
        public static LLVMValueRef NextGlobalIFunc(this LLVMValueRef self) => (self.Handle != default) ? LLVM.GetNextGlobalIFunc(self) : default;

        /// <summary>Convenience wrapper for LLVM.GetMDString.</summary>
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

        /// <summary>Convenience wrapper for LLVM.GlobalCopyAllMetadata.</summary>
        public static (LLVMValueMetadataEntryRef MetadataRef, uint Count) GlobalCopyAllMetadata(this LLVMValueRef self)
        {
            UIntPtr count;
            LLVMValueMetadataEntryRef metadataRef = LLVM.GlobalCopyAllMetadata(self, &count);
            return (metadataRef, (uint)count);
        }

        public static LLVMMetadataRef GetSubprogram(this LLVMValueRef self)
        {
            return LLVM.GetSubprogram(self);
        }

        public static void SetSubprogram(this LLVMValueRef self, LLVMMetadataRef subprogram)
        {
            LLVM.SetSubprogram(self, subprogram);
        }

        public static LLVMMetadataRef ValueAsMetadata(this LLVMValueRef self)
        {
            return LLVM.ValueAsMetadata(self);
        }

        public static LLVMValueRef[] GetMDNodeOperands(this LLVMValueRef self)
        {
            var Dest = new LLVMValueRef[self.GetMDNodeNumOperands()];

            fixed (LLVMValueRef* pDest = Dest)
            {
                LLVM.GetMDNodeOperands(self, (LLVMOpaqueValue**)pDest);
            }

            return Dest;
        }

        public static uint GetMDNodeNumOperands(this LLVMValueRef self)
        {
            return LLVM.GetMDNodeNumOperands(self);
        }
    }
}
