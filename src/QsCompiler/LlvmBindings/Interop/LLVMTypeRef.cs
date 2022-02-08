// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMTypeRef : IEquatable<LLVMTypeRef>
    {
        public IntPtr Handle;

        public LLVMTypeRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static LLVMTypeRef BFloat => LLVM.BFloatType();

        public static LLVMTypeRef Double => LLVM.DoubleType();

        public static LLVMTypeRef Float => LLVM.FloatType();

        public static LLVMTypeRef FP128 => LLVM.FP128Type();

        public static LLVMTypeRef Half => LLVM.HalfType();

        public static LLVMTypeRef Int1 => LLVM.Int1Type();

        public static LLVMTypeRef Int8 => LLVM.Int8Type();

        public static LLVMTypeRef Int16 => LLVM.Int16Type();

        public static LLVMTypeRef Int32 => LLVM.Int32Type();

        public static LLVMTypeRef Int64 => LLVM.Int64Type();

        public static LLVMTypeRef Label => LLVM.LabelType();

        public static LLVMTypeRef PPCFP128 => LLVM.PPCFP128Type();

        public static LLVMTypeRef Void => LLVM.VoidType();

        public static LLVMTypeRef X86FP80 => LLVM.X86FP80Type();

        public static LLVMTypeRef X86MMX => LLVM.X86MMXType();

        public static LLVMTypeRef X86AMX => LLVM.X86AMXType();

        public LLVMValueRef AlignOf => (this.Handle != IntPtr.Zero) ? LLVM.AlignOf(this) : default;

        public uint ArrayLength => (this.Kind == LLVMTypeKind.LLVMArrayTypeKind) ? LLVM.GetArrayLength(this) : default;

        public LLVMContextRef Context => (this.Handle != IntPtr.Zero) ? LLVM.GetTypeContext(this) : default;

        public LLVMTypeRef ElementType => ((this.Kind == LLVMTypeKind.LLVMPointerTypeKind) || (this.Kind == LLVMTypeKind.LLVMArrayTypeKind) || (this.Kind == LLVMTypeKind.LLVMVectorTypeKind)) ? LLVM.GetElementType(this) : default;

        public uint IntWidth => (this.Kind == LLVMTypeKind.LLVMIntegerTypeKind) ? LLVM.GetIntTypeWidth(this) : default;

        public bool IsFunctionVarArg => (this.Kind == LLVMTypeKind.LLVMFunctionTypeKind) && LLVM.IsFunctionVarArg(this) != 0;

        public bool IsOpaqueStruct => (this.Kind == LLVMTypeKind.LLVMStructTypeKind) && LLVM.IsOpaqueStruct(this) != 0;

        public bool IsPackedStruct => (this.Kind == LLVMTypeKind.LLVMStructTypeKind) && LLVM.IsPackedStruct(this) != 0;

        public bool IsSized => (this.Handle != IntPtr.Zero) && LLVM.TypeIsSized(this) != 0;

        public LLVMTypeKind Kind => (this.Handle != IntPtr.Zero) ? LLVM.GetTypeKind(this) : default;

        public LLVMTypeRef[] ParamTypes
        {
            get
            {
                if (this.Kind != LLVMTypeKind.LLVMFunctionTypeKind)
                {
                    return Array.Empty<LLVMTypeRef>();
                }

                var dest = new LLVMTypeRef[this.ParamTypesCount];

                fixed (LLVMTypeRef* pDest = dest)
                {
                    LLVM.GetParamTypes(this, (LLVMOpaqueType**)pDest);
                }

                return dest;
            }
        }

        public uint ParamTypesCount => (this.Kind == LLVMTypeKind.LLVMFunctionTypeKind) ? LLVM.CountParamTypes(this) : default;

        public uint PointerAddressSpace => (this.Kind == LLVMTypeKind.LLVMPointerTypeKind) ? LLVM.GetPointerAddressSpace(this) : default;

        public LLVMValueRef Poison => (this.Handle != IntPtr.Zero) ? LLVM.GetPoison(this) : default;

        public LLVMTypeRef ReturnType => (this.Kind == LLVMTypeKind.LLVMFunctionTypeKind) ? LLVM.GetReturnType(this) : default;

        public LLVMValueRef SizeOf => (this.Handle != IntPtr.Zero) ? LLVM.SizeOf(this) : default;

        public LLVMTypeRef[] StructElementTypes
        {
            get
            {
                if (this.Kind != LLVMTypeKind.LLVMStructTypeKind)
                {
                    return Array.Empty<LLVMTypeRef>();
                }

                var dest = new LLVMTypeRef[this.StructElementTypesCount];

                fixed (LLVMTypeRef* pDest = dest)
                {
                    LLVM.GetStructElementTypes(this, (LLVMOpaqueType**)pDest);
                }

                return dest;
            }
        }

        public uint StructElementTypesCount => (this.Kind == LLVMTypeKind.LLVMStructTypeKind) ? LLVM.CountStructElementTypes(this) : default;

        public string StructName
        {
            get
            {
                if (this.Kind != LLVMTypeKind.LLVMStructTypeKind)
                {
                    return string.Empty;
                }

                var pStructName = LLVM.GetStructName(this);

                if (pStructName == null)
                {
                    return string.Empty;
                }

                var span = new ReadOnlySpan<byte>(pStructName, int.MaxValue);
                return span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            }
        }

        public LLVMTypeRef[] Subtypes
        {
            get
            {
                if (this.Handle == IntPtr.Zero)
                {
                    return Array.Empty<LLVMTypeRef>();
                }

                var arr = new LLVMTypeRef[this.SubtypesCount];

                fixed (LLVMTypeRef* pArr = arr)
                {
                    LLVM.GetSubtypes(this, (LLVMOpaqueType**)pArr);
                }

                return arr;
            }
        }

        public uint SubtypesCount => (this.Handle != IntPtr.Zero) ? LLVM.GetNumContainedTypes(this) : default;

        public LLVMValueRef Undef => (this.Handle != IntPtr.Zero) ? LLVM.GetUndef(this) : default;

        public uint VectorSize => (this.Kind == LLVMTypeKind.LLVMVectorTypeKind) ? LLVM.GetVectorSize(this) : default;

        public static implicit operator LLVMTypeRef(LLVMOpaqueType* value) => new LLVMTypeRef((IntPtr)value);

        public static implicit operator LLVMOpaqueType*(LLVMTypeRef value) => (LLVMOpaqueType*)value.Handle;

        public static bool operator ==(LLVMTypeRef left, LLVMTypeRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMTypeRef left, LLVMTypeRef right) => !(left == right);

        public static LLVMTypeRef CreateFunction(LLVMTypeRef returnType, LLVMTypeRef[] paramTypes, bool isVarArg = false) => CreateFunction(returnType, paramTypes.AsSpan(), isVarArg);

        public static LLVMTypeRef CreateFunction(LLVMTypeRef returnType, ReadOnlySpan<LLVMTypeRef> paramTypes, bool isVarArg)
        {
            fixed (LLVMTypeRef* pParamTypes = paramTypes)
            {
                return LLVM.FunctionType(returnType, (LLVMOpaqueType**)pParamTypes, (uint)paramTypes.Length, isVarArg ? 1 : 0);
            }
        }

        public static LLVMTypeRef CreateArray(LLVMTypeRef elementType, uint elementCount) => LLVM.ArrayType(elementType, elementCount);

        public static LLVMTypeRef CreateInt(uint numBits) => LLVM.IntType(numBits);

        public static LLVMTypeRef CreateIntPtr(LLVMTargetDataRef tD) => LLVM.IntPtrType(tD);

        public static LLVMTypeRef CreateIntPtrForAS(LLVMTargetDataRef tD, uint aS) => LLVM.IntPtrTypeForAS(tD, aS);

        public static LLVMTypeRef CreatePointer(LLVMTypeRef elementType, uint addressSpace) => LLVM.PointerType(elementType, addressSpace);

        public static LLVMTypeRef CreateStruct(LLVMTypeRef[] elementTypes, bool packed) => CreateStruct(elementTypes.AsSpan(), packed);

        public static LLVMTypeRef CreateStruct(ReadOnlySpan<LLVMTypeRef> elementTypes, bool packed)
        {
            fixed (LLVMTypeRef* pElementTypes = elementTypes)
            {
                return LLVM.StructType((LLVMOpaqueType**)pElementTypes, (uint)elementTypes.Length, packed ? 1 : 0);
            }
        }

        public static LLVMTypeRef CreateVector(LLVMTypeRef elementType, uint elementCount) => LLVM.VectorType(elementType, elementCount);

        public static LLVMTypeRef CreateScalableVector(LLVMTypeRef elementType, uint elementCount) => LLVM.ScalableVectorType(elementType, elementCount);

        public void Dump() => LLVM.DumpType(this);

        public override bool Equals(object obj) => (obj is LLVMTypeRef other) && this.Equals(other);

        public bool Equals(LLVMTypeRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public string PrintToString()
        {
            var pStr = LLVM.PrintTypeToString(this);

            if (pStr == null)
            {
                return string.Empty;
            }

            var span = new ReadOnlySpan<byte>(pStr, int.MaxValue);

            var result = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public LLVMTypeRef StructGetTypeAtIndex(uint index) => LLVM.StructGetTypeAtIndex(this, index);

        public void StructSetBody(LLVMTypeRef[] elementTypes, bool packed) => this.StructSetBody(elementTypes.AsSpan(), packed);

        public void StructSetBody(ReadOnlySpan<LLVMTypeRef> elementTypes, bool packed)
        {
            fixed (LLVMTypeRef* pElementTypes = elementTypes)
            {
                LLVM.StructSetBody(this, (LLVMOpaqueType**)pElementTypes, (uint)elementTypes.Length, packed ? 1 : 0);
            }
        }

        public override string ToString() => (this.Handle != IntPtr.Zero) ? this.PrintToString() : string.Empty;
    }
}
