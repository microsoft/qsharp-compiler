// <copyright file="LLVMMemoryBufferRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMMetadataRef class.</summary>
    public static unsafe class LLVMMetadataRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.GetMetadataKind.</summary>
        public static LLVMMetadataKind GetMetadataKind(this LLVMMetadataRef self)
        {
            return (LLVMMetadataKind)LLVM.GetMetadataKind(self);
        }

        public static void ReplaceAllUsesWith(this LLVMMetadataRef self, LLVMMetadataRef replacement)
        {
            LLVM.MetadataReplaceAllUsesWith(self, replacement);
        }

        public static LLVMMetadataRef DIGlobalVariableExpressionGetVariable(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetVariable(self);
        }

        public static LLVMMetadataRef DIGlobalVariableExpressionGetExpression(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetExpression(self);
        }

        public static string? DIFileGetFilename(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetFilename(self, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static string? DIFileGetDirectory(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetDirectory(self, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static string? DIFileGetSource(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetSource(self, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static LLVMMetadataRef DILocationGetScope(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetScope(self);
        }

        public static uint DILocationGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetLine(self);
        }

        public static uint DILocationGetColumn(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetColumn(self);
        }

        public static LLVMMetadataRef DILocationGetInlinedAt(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetInlinedAt(self);
        }

        public static LLVMMetadataRef DIScopeGetFile(this LLVMMetadataRef self)
        {
            return LLVM.DIScopeGetFile(self);
        }

        public static uint DISubprogramGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DISubprogramGetLine(self);
        }

        public static uint DITypeGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetLine(self);
        }

        public static ulong DITypeGetSizeInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetSizeInBits(self);
        }

        public static uint DITypeGetAlignInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetAlignInBits(self);
        }

        public static ulong DITypeGetOffsetInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetOffsetInBits(self);
        }

        public static LLVMDIFlags DITypeGetFlags(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetFlags(self);
        }

        public static uint DIVariableGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetLine(self);
        }

        public static LLVMMetadataRef DIVariableGetFile(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetFile(self);
        }

        public static LLVMMetadataRef DIVariableGetScope(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetScope(self);
        }
    }
}
