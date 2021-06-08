// <copyright file="LLVMDIBuilderRefExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;

namespace LLVMSharp.Interop
{
    /// <summary>Extensions for <see cref="LLVMDIBuilderRef"/>.</summary>
    public static unsafe class LLVMDIBuilderRefExtensions
    {
        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateNameSpace"/>.</summary>
        public static LLVMMetadataRef CreateNameSpace(this LLVMDIBuilderRef self, LLVMMetadataRef parentScope, string name, bool exportSymbols)
        {
            return LLVM.DIBuilderCreateNameSpace(self, parentScope, name.AsMarshaledString(), (UIntPtr)name.Length, exportSymbols ? 1 : 0);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateLexicalBlock"/>.</summary>
        public static LLVMMetadataRef CreateLexicalBlock(this LLVMDIBuilderRef self, LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint column)
        {
            return LLVM.DIBuilderCreateLexicalBlock(self, scope, file, line, column);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateLexicalBlockFile"/>.</summary>
        public static LLVMMetadataRef CreateLexicalBlockFile(this LLVMDIBuilderRef self, LLVMMetadataRef scope, LLVMMetadataRef file, uint discriminator)
        {
            return LLVM.DIBuilderCreateLexicalBlockFile(self, scope, file, discriminator);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateAutoVariable"/>.</summary>
        public static LLVMMetadataRef CreateAutoVariable(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags diflags, uint alignInBits)
        {
            return LLVM.DIBuilderCreateAutoVariable(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, ty, alwaysPreserve ? 1 : 0, diflags, alignInBits);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateParameterVariable"/>.</summary>
        public static LLVMMetadataRef CreateParameterVariable(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, uint argNo, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool alwaysPreserve, LLVMDIFlags diflags)
        {
            return LLVM.DIBuilderCreateParameterVariable(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, argNo, file, line, ty, alwaysPreserve ? 1 : 0, diflags);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateBasicType"/>.</summary>
        public static LLVMMetadataRef CreateBasicType(this LLVMDIBuilderRef self, string name, ulong sizeInBits, uint encoding, LLVMDIFlags diflags)
        {
            return LLVM.DIBuilderCreateBasicType(self, name.AsMarshaledString(), (UIntPtr)name.Length, sizeInBits, encoding, diflags);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreatePointerType"/>.</summary>
        public static LLVMMetadataRef CreatePointerType(this LLVMDIBuilderRef self, LLVMMetadataRef pointeeTy, ulong sizeInBits, uint alignInBits, uint addressSpace, string? name)
        {
            if (name == null)
            {
                name = string.Empty;
            }

            return LLVM.DIBuilderCreatePointerType(self, pointeeTy, sizeInBits, alignInBits, addressSpace, name.AsMarshaledString(), (UIntPtr)name.Length);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateQualifiedType"/>.</summary>
        public static LLVMMetadataRef CreateQualifiedType(this LLVMDIBuilderRef self, uint tag, LLVMMetadataRef type)
        {
            return LLVM.DIBuilderCreateQualifiedType(self, tag, type);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderGetOrCreateTypeArray"/>.</summary>
        public static LLVMMetadataRef GetOrCreateTypeArray(this LLVMDIBuilderRef self, LLVMMetadataRef[] data, long numElements)
        {
            fixed (LLVMMetadataRef* pData = data.AsSpan())
            {
                return LLVM.DIBuilderGetOrCreateTypeArray(self, (LLVMOpaqueMetadata**)pData, (UIntPtr)numElements);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateStructType"/>.</summary>
        public static LLVMMetadataRef CreateStructType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef derivedFrom, LLVMMetadataRef[] elements, uint numElements, uint runTimeLang, LLVMMetadataRef vTableHolder, string uniqueId)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateStructType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, flags, derivedFrom, (LLVMOpaqueMetadata**)pElements, numElements, runTimeLang, vTableHolder, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateUnionType"/>.</summary>
        public static LLVMMetadataRef CreateUnionType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, LLVMMetadataRef[] elements, uint numElements, uint runTimeLang, string uniqueId)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateUnionType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, flags, (LLVMOpaqueMetadata**)pElements, numElements, runTimeLang, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateMemberType"/>.</summary>
        public static LLVMMetadataRef CreateMemberType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, ulong offsetInBits, LLVMDIFlags flags, LLVMMetadataRef ty)
        {
            return LLVM.DIBuilderCreateMemberType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, offsetInBits, flags, ty);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateArrayType"/>.</summary>
        public static LLVMMetadataRef CreateArrayType(this LLVMDIBuilderRef self, ulong size, uint alignInBits, LLVMMetadataRef ty, LLVMMetadataRef[] subscripts, uint numSubscripts)
        {
            fixed (LLVMMetadataRef* pSubscripts = subscripts.AsSpan())
            {
                return LLVM.DIBuilderCreateArrayType(self, size, alignInBits, ty, (LLVMOpaqueMetadata**)pSubscripts, numSubscripts);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateVectorType"/>.</summary>
        public static LLVMMetadataRef CreateVectorType(this LLVMDIBuilderRef self, ulong size, uint alignInBits, LLVMMetadataRef ty, LLVMMetadataRef[] subscripts, uint numSubscripts)
        {
            fixed (LLVMMetadataRef* pSubscripts = subscripts.AsSpan())
            {
                return LLVM.DIBuilderCreateVectorType(self, size, alignInBits, ty, (LLVMOpaqueMetadata**)pSubscripts, numSubscripts);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderGetOrCreateSubrange"/>.</summary>
        public static LLVMMetadataRef GetOrCreateSubrange(this LLVMDIBuilderRef self, long lowerBound, long count)
        {
            return LLVM.DIBuilderGetOrCreateSubrange(self, lowerBound, count);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderGetOrCreateArray"/>.</summary>
        public static LLVMMetadataRef GetOrCreateArray(this LLVMDIBuilderRef self, LLVMMetadataRef[] data, long numElements)
        {
            fixed (LLVMMetadataRef* pData = data.AsSpan())
            {
                return LLVM.DIBuilderGetOrCreateArray(self, (LLVMOpaqueMetadata**)pData, (UIntPtr)numElements);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateEnumerator"/>.</summary>
        public static LLVMMetadataRef CreateEnumerator(this LLVMDIBuilderRef self, string name, long value, bool isUnsigned)
        {
            return LLVM.DIBuilderCreateEnumerator(self, name.AsMarshaledString(), (UIntPtr)name.Length, value, isUnsigned ? 1 : 0);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateEnumerationType"/>.</summary>
        public static LLVMMetadataRef CreateEnumerationType(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, ulong sizeInBits, uint alignInBits, LLVMMetadataRef[] elements, uint numElements, LLVMMetadataRef classTy)
        {
            fixed (LLVMMetadataRef* pElements = elements.AsSpan())
            {
                return LLVM.DIBuilderCreateEnumerationType(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, file, line, sizeInBits, alignInBits, (LLVMOpaqueMetadata**)pElements, numElements, classTy);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateGlobalVariableExpression"/>.</summary>
        public static LLVMMetadataRef CreateGlobalVariableExpression(this LLVMDIBuilderRef self, LLVMMetadataRef scope, string name, string linkage, LLVMMetadataRef file, uint line, LLVMMetadataRef ty, bool localToUnit, LLVMMetadataRef expr, LLVMMetadataRef decl, uint alignInBits)
        {
            return LLVM.DIBuilderCreateGlobalVariableExpression(self, scope, name.AsMarshaledString(), (UIntPtr)name.Length, linkage.AsMarshaledString(), (UIntPtr)linkage.Length, file, line, ty, localToUnit ? 1 : 0, expr, decl, alignInBits);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderInsertDeclareBefore"/>.</summary>
        public static LLVMValueRef InsertDeclareBefore(this LLVMDIBuilderRef self, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMValueRef instruction)
        {
            return LLVM.DIBuilderInsertDeclareBefore(self, storage, varInfo, expr, debugLoc, instruction);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderInsertDeclareAtEnd"/>.</summary>
        public static LLVMValueRef InsertDeclareAtEnd(this LLVMDIBuilderRef self, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMBasicBlockRef block)
        {
            return LLVM.DIBuilderInsertDeclareAtEnd(self, storage, varInfo, expr, debugLoc, block);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderInsertDbgValueBefore"/>.</summary>
        public static LLVMValueRef InsertDbgValueBefore(this LLVMDIBuilderRef self, LLVMValueRef val, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMValueRef instruction)
        {
            return LLVM.DIBuilderInsertDbgValueBefore(self, val, varInfo, expr, debugLoc, instruction);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderInsertDbgValueAtEnd"/>.</summary>
        public static LLVMValueRef InsertDbgValueAtEnd(this LLVMDIBuilderRef self, LLVMValueRef val, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMBasicBlockRef block)
        {
            return LLVM.DIBuilderInsertDbgValueAtEnd(self, val, varInfo, expr, debugLoc, block);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateExpression"/>.</summary>
        public static LLVMMetadataRef CreateExpression(this LLVMDIBuilderRef self, long[] addr, long length)
        {
            fixed (long* pAddr = addr.AsSpan())
            {
                return LLVM.DIBuilderCreateExpression(self, pAddr, (UIntPtr)length);
            }
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateConstantValueExpression"/>.</summary>
        public static LLVMMetadataRef CreateConstantValueExpression(this LLVMDIBuilderRef self, long value)
        {
            return LLVM.DIBuilderCreateConstantValueExpression(self, value);
        }

        /// <summary>Convenience wrapper for <see cref="LLVM.DIBuilderCreateReplaceableCompositeType"/>.</summary>
        public static LLVMMetadataRef CreateReplaceableCompositeType(this LLVMDIBuilderRef self, uint tag, string name, LLVMMetadataRef scope, LLVMMetadataRef file, uint line, uint runTimeLang, ulong sizeInBits, uint alignInBits, LLVMDIFlags flags, string uniqueId)
        {
            return LLVM.DIBuilderCreateReplaceableCompositeType(self, tag, name.AsMarshaledString(), (UIntPtr)name.Length, scope, file, line, runTimeLang, sizeInBits, alignInBits, flags, uniqueId.AsMarshaledString(), (UIntPtr)uniqueId.Length);
        }
    }
}
