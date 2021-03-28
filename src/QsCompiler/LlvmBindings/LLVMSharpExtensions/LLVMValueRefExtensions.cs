// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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

        // /// <summary>Convenience wrapper for LLVM.InstructionGetAllMetadataOtherThanDebugLoc</summary>
        // public static (LLVMValueMetadataEntryRef entries, uint numEntries) InstructionGetAllMetadataOtherThanDebugLoc( this LLVMValueRef self )
        // {
        //     UIntPtr numEntries;
        //     LLVMValueMetadataEntryRef entries = LLVM.InstructionGetAllMetadataOtherThanDebugLoc( self, &numEntries );
        //     return ( entries, (uint)numEntries );
        // }
    }
}
