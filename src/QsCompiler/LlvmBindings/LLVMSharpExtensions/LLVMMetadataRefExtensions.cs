// <copyright file="LLVMMetadataRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMMetadataRef"/>.</summary>
    public static unsafe class LLVMMetadataRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.GetMetadataKind"/>.</summary>
        public static LLVMMetadataKind GetMetadataKind(this LLVMMetadataRef self)
        {
            return (LLVMMetadataKind)LLVM.GetMetadataKind(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.MetadataReplaceAllUsesWith"/>.</summary>
        public static void ReplaceAllUsesWith(this LLVMMetadataRef self, LLVMMetadataRef replacement)
        {
            LLVM.MetadataReplaceAllUsesWith(self, replacement);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIGlobalVariableExpressionGetVariable"/>.</summary>
        public static LLVMMetadataRef DIGlobalVariableExpressionGetVariable(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetVariable(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIGlobalVariableExpressionGetExpression"/>.</summary>
        public static LLVMMetadataRef DIGlobalVariableExpressionGetExpression(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetExpression(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIFileGetFilename"/>.</summary>
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

        /// <summary>Convenience wrapper for <see cref="LLVM.DIFileGetDirectory"/>.</summary>
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

        /// <summary>Convenience wrapper for <see cref="LLVM.DIFileGetSource"/>.</summary>
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

        /// <summary>Convenience wrapper for <see cref="LLVM.DILocationGetScope"/>.</summary>
        public static LLVMMetadataRef DILocationGetScope(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetScope(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DILocationGetLine"/>.</summary>
        public static uint DILocationGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetLine(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DILocationGetColumn"/>.</summary>
        public static uint DILocationGetColumn(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetColumn(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DILocationGetInlinedAt"/>.</summary>
        public static LLVMMetadataRef DILocationGetInlinedAt(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetInlinedAt(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIScopeGetFile"/>.</summary>
        public static LLVMMetadataRef DIScopeGetFile(this LLVMMetadataRef self)
        {
            return LLVM.DIScopeGetFile(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DISubprogramGetLine"/>.</summary>
        public static uint DISubprogramGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DISubprogramGetLine(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DITypeGetLine"/>.</summary>
        public static uint DITypeGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetLine(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DITypeGetSizeInBits"/>.</summary>
        public static ulong DITypeGetSizeInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetSizeInBits(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DITypeGetAlignInBits"/>.</summary>
        public static uint DITypeGetAlignInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetAlignInBits(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DITypeGetOffsetInBits"/>.</summary>
        public static ulong DITypeGetOffsetInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetOffsetInBits(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DITypeGetFlags"/>.</summary>
        public static LLVMDIFlags DITypeGetFlags(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetFlags(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIVariableGetLine"/>.</summary>
        public static uint DIVariableGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetLine(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIVariableGetFile"/>.</summary>
        public static LLVMMetadataRef DIVariableGetFile(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetFile(self);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIVariableGetScope"/>.</summary>
        public static LLVMMetadataRef DIVariableGetScope(this LLVMMetadataRef self)
        {
            return LLVM.DIVariableGetScope(self);
        }
    }
}
