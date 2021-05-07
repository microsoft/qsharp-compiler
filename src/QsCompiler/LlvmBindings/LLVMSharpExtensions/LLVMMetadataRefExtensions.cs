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
            return (LLVMMetadataKind)LLVM.GetMetadataKind((LLVMOpaqueMetadata*)self.Handle);
        }

        public static void ReplaceAllUsesWith(this LLVMMetadataRef self, LLVMMetadataRef replacement)
        {
            LLVM.MetadataReplaceAllUsesWith((LLVMOpaqueMetadata*)self.Handle, (LLVMOpaqueMetadata*)replacement.Handle);
        }

        public static LLVMMetadataRef DIGlobalVariableExpressionGetVariable(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetVariable((LLVMOpaqueMetadata*)self.Handle);
        }

        public static LLVMMetadataRef DIGlobalVariableExpressionGetExpression(this LLVMMetadataRef self)
        {
            return LLVM.DIGlobalVariableExpressionGetExpression((LLVMOpaqueMetadata*)self.Handle);
        }

        public static string? DIFileGetFilename(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetFilename((LLVMOpaqueMetadata*)self.Handle, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static string? DIFileGetDirectory(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetDirectory((LLVMOpaqueMetadata*)self.Handle, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static string? DIFileGetSource(this LLVMMetadataRef self)
        {
            uint len;
            var pStr = LLVM.DIFileGetSource((LLVMOpaqueMetadata*)self.Handle, &len);
            if (pStr == default)
            {
                return null;
            }

            return new ReadOnlySpan<byte>(pStr, (int)len).AsString();
        }

        public static LLVMMetadataRef DILocationGetScope(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetScope((LLVMOpaqueMetadata*)self.Handle);
        }

        public static uint DILocationGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetLine((LLVMOpaqueMetadata*)self.Handle);
        }

        public static uint DILocationGetColumn(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetColumn((LLVMOpaqueMetadata*)self.Handle);
        }

        public static LLVMMetadataRef DILocationGetInlinedAt(this LLVMMetadataRef self)
        {
            return LLVM.DILocationGetInlinedAt((LLVMOpaqueMetadata*)self.Handle);
        }

        public static LLVMMetadataRef DIScopeGetFile(this LLVMMetadataRef self)
        {
            return LLVM.DIScopeGetFile((LLVMOpaqueMetadata*)self.Handle);
        }

        public static uint DISubprogramGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DISubprogramGetLine((LLVMOpaqueMetadata*)self.Handle);
        }

        public static uint DITypeGetLine(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetLine((LLVMOpaqueMetadata*)self.Handle);
        }

        public static ulong DITypeGetSizeInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetSizeInBits((LLVMOpaqueMetadata*)self.Handle);
        }

        public static uint DITypeGetAlignInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetAlignInBits((LLVMOpaqueMetadata*)self.Handle);
        }

        public static ulong DITypeGetOffsetInBits(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetOffsetInBits((LLVMOpaqueMetadata*)self.Handle);
        }

        public static LLVMDIFlags DITypeGetFlags(this LLVMMetadataRef self)
        {
            return LLVM.DITypeGetFlags((LLVMOpaqueMetadata*)self.Handle);
        }
    }
}
