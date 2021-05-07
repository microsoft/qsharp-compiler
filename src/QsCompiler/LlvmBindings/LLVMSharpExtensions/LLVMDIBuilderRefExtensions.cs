// <copyright file="LLVMMemoryBufferRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for the LLVMSharp.Interop.LLVMDIBuilderRef class.</summary>
    public static unsafe class LLVMDIBuilderRefExtensions
    {
        /// <summary>Convenience wrapper for LLVM.DIBuilderCreateNameSpace.</summary>
        public static LLVMMetadataRef CreateNameSpace(this LLVMDIBuilderRef self, LLVMMetadataRef parentScope, string name, bool exportSymbols)
        {
            return LLVM.DIBuilderCreateNameSpace(self, parentScope, name.AsMarshaledString(), (UIntPtr)name.Length, exportSymbols ? 1 : 0);
        }

        public static LLVMMetadataRef CreateLexicalBlock(this LLVMDIBuilderRef self, LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint column)
        {
            return LLVM.DIBuilderCreateLexicalBlock(self, scope, file, line, column);
        }

        public static LLVMMetadataRef CreateLexicalBlockFile(this LLVMDIBuilderRef self, LLVMMetadataRef scope, LLVMMetadataRef file, uint discriminator)
        {
            return LLVM.DIBuilderCreateLexicalBlockFile(self, scope, file, discriminator);
        }

        public static LLVMMetadataRef CreateAutoVariable(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags diflags, uint alignInBits)
        {
            return LLVM.DIBuilderCreateAutoVariable(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, ty, alwaysPreserve ? 1 : 0, diflags, alignInBits);
        }

        public static LLVMMetadataRef CreateParameterVariable(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, uint argNo, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags diflags)
        {
            return LLVM.DIBuilderCreateParameterVariable(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, argNo, file, line, ty, alwaysPreserve ? 1 : 0, diflags);
        }

        public static LLVMMetadataRef CreateBasicType(this LLVMDIBuilderRef self, string name, ulong sizeInBits, uint encoding, LLVMDIFlags diflags)
        {
            return LLVM.DIBuilderCreateBasicType(self, name.AsMarshaledString(), (UIntPtr)name.Length, sizeInBits, encoding, diflags);
        }

        public static LLVMMetadataRef CreatePointerType(this LLVMDIBuilderRef self, LLVMMetadataRef pointeeTy, ulong sizeInBits, uint alignInBits, uint addressSpace, string? name)
        {
            if (name == null)
            {
                name = string.Empty;
            }

            return LLVM.DIBuilderCreatePointerType(self, pointeeTy, sizeInBits, alignInBits, addressSpace, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        public static LLVMMetadataRef CreateQualifiedType(this LLVMDIBuilderRef self, uint tag, LLVMMetadataRef type)
        {
            return LLVM.DIBuilderCreateQualifiedType(self, tag, type);
        }

        public static LLVMMetadataRef GetOrCreateTypeArray(this LLVMDIBuilderRef self, LLVMMetadataRef[] data, long numElements)
        {
            fixed (LLVMMetadataRef* pData = data.AsSpan())
            {
                return LLVM.DIBuilderGetOrCreateTypeArray(self, (LLVMOpaqueMetadata**)pData, (UIntPtr)numElements);
            }
        }

        public static LLVMMetadataRef CreateStructType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef derivedFrom, LLVMMetadataRef[] elements, uint numElements, uint runTimeLang, LLVMMetadataRef vTableHolder, string uniqueId)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateStructType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, flags, derivedFrom, (LLVMOpaqueMetadata**)pElements, numElements, runTimeLang, vTableHolder, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
            }
        }

        public static LLVMMetadataRef CreateUnionType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef[] elements, uint numElements, uint runTimeLang, string uniqueId)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateUnionType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, flags, (LLVMOpaqueMetadata**)pElements, numElements, runTimeLang, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
            }
        }

        public static LLVMMetadataRef CreateMemberType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, ulong offsetInBits, LLVMDIFlags flags, LLVMMetadataRef ty)
        {
            return LLVM.DIBuilderCreateMemberType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, offsetInBits, flags, ty);
        }

        public static LLVMMetadataRef CreateArrayType(this LLVMDIBuilderRef self, ulong size, uint alignInBits, LLVMMetadataRef ty, LLVMMetadataRef[] subscripts, uint numSubscripts)
        {
            fixed (LLVMMetadataRef* pSubscripts = subscripts.AsSpan())
            {
                return LLVM.DIBuilderCreateArrayType(self, size, alignInBits, ty, (LLVMOpaqueMetadata**)pSubscripts, numSubscripts);
            }
        }

        public static LLVMMetadataRef CreateVectorType(this LLVMDIBuilderRef self, ulong size, uint alignInBits, LLVMMetadataRef ty, LLVMMetadataRef[] subscripts, uint numSubscripts)
        {
            fixed (LLVMMetadataRef* pSubscripts = subscripts.AsSpan())
            {
                return LLVM.DIBuilderCreateVectorType(self, size, alignInBits, ty, (LLVMOpaqueMetadata**)pSubscripts, numSubscripts);
            }
        }

        public static LLVMMetadataRef GetOrCreateSubrange(this LLVMDIBuilderRef self, long lowerBound, long count)
        {
            return LLVM.DIBuilderGetOrCreateSubrange(self, lowerBound, count);
        }

        public static LLVMMetadataRef GetOrCreateArray(this LLVMDIBuilderRef self, LLVMMetadataRef[] data, long numElements)
        {
            fixed (LLVMMetadataRef* pData = data.AsSpan())
            {
                return LLVM.DIBuilderGetOrCreateArray(self, (LLVMOpaqueMetadata**)pData, (UIntPtr)numElements);
            }
        }

        public static LLVMMetadataRef CreateEnumerator(this LLVMDIBuilderRef self, string name, long value, bool isUnsigned)
        {
            return LLVM.DIBuilderCreateEnumerator(self, name.AsMarshaledString(), (UIntPtr)name.Length, value, isUnsigned ? 1 : 0);
        }

        public static LLVMMetadataRef CreateEnumerationType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMMetadataRef[] elements, uint numElements, LLVMMetadataRef classTy)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateEnumerationType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, (LLVMOpaqueMetadata**)pElements, numElements, classTy);
            }
        }

        public static LLVMMetadataRef CreateGlobalVariableExpression(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, string linkage, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool localToUnit, LLVMMetadataRef expr, LLVMMetadataRef decl, uint alignInBits)
        {
            return LLVM.DIBuilderCreateGlobalVariableExpression(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, linkage.AsMarshaledString(), (UIntPtr)linkage.Length, file, line, ty, localToUnit ? 1 : 0, expr, decl, alignInBits);
        }

        public static LLVMValueRef InsertDeclareBefore(this LLVMDIBuilderRef self, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMValueRef instruction)
        {
            return LLVM.DIBuilderInsertDeclareBefore(self, storage, varInfo, expr, debugLoc, instruction);
        }

        public static LLVMValueRef InsertDeclareAtEnd(this LLVMDIBuilderRef self, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMBasicBlockRef block)
        {
            return LLVM.DIBuilderInsertDeclareAtEnd(self, storage, varInfo, expr, debugLoc, block);
        }

        public static LLVMValueRef InsertDbgValueBefore(this LLVMDIBuilderRef self, LLVMValueRef val, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMValueRef instruction)
        {
            return LLVM.DIBuilderInsertDbgValueBefore(self, val, varInfo, expr, debugLoc, instruction);
        }

        public static LLVMValueRef InsertDbgValueAtEnd(this LLVMDIBuilderRef self, LLVMValueRef val, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMBasicBlockRef block)
        {
            return LLVM.DIBuilderInsertDbgValueAtEnd(self, val, varInfo, expr, debugLoc, block);
        }

        public static LLVMMetadataRef CreateExpression(this LLVMDIBuilderRef self, long[] addr, long length)
        {
            fixed (long* pAddr = addr.AsSpan())
            {
                return LLVM.DIBuilderCreateExpression(self, pAddr, (UIntPtr)length);
            }
        }

        public static LLVMMetadataRef CreateConstantValueExpression(this LLVMDIBuilderRef self, long value)
        {
            return LLVM.DIBuilderCreateConstantValueExpression(self, value);
        }

        public static LLVMMetadataRef CreateReplaceableCompositeType(this LLVMDIBuilderRef self, uint tag, string name, LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint runTimeLang, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, string uniqueId)
        {
            return LLVM.DIBuilderCreateReplaceableCompositeType(self, tag, name.AsMarshaledString(), (UIntPtr)name.Length, scope, file, line, runTimeLang, sizeInBits, alignInBits, flags, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
        }
    }
}
