// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;
using System.Runtime.InteropServices;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMContextRef : IDisposable, IEquatable<LLVMContextRef>
    {
        public IntPtr Handle;

        public LLVMContextRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static LLVMContextRef Global => LLVM.GetGlobalContext();

        public LLVMTypeRef BFloatType => (this.Handle != IntPtr.Zero) ? LLVM.BFloatTypeInContext(this) : default;

        public LLVMTypeRef DoubleType => (this.Handle != IntPtr.Zero) ? LLVM.DoubleTypeInContext(this) : default;

        public LLVMTypeRef FloatType => (this.Handle != IntPtr.Zero) ? LLVM.FloatTypeInContext(this) : default;

        public LLVMTypeRef HalfType => (this.Handle != IntPtr.Zero) ? LLVM.HalfTypeInContext(this) : default;

        public LLVMTypeRef Int1Type => (this.Handle != IntPtr.Zero) ? LLVM.Int1TypeInContext(this) : default;

        public LLVMTypeRef Int8Type => (this.Handle != IntPtr.Zero) ? LLVM.Int8TypeInContext(this) : default;

        public LLVMTypeRef Int16Type => (this.Handle != IntPtr.Zero) ? LLVM.Int16TypeInContext(this) : default;

        public LLVMTypeRef Int32Type => (this.Handle != IntPtr.Zero) ? LLVM.Int32TypeInContext(this) : default;

        public LLVMTypeRef Int64Type => (this.Handle != IntPtr.Zero) ? LLVM.Int64TypeInContext(this) : default;

        public LLVMTypeRef FP128Type => (this.Handle != IntPtr.Zero) ? LLVM.FP128TypeInContext(this) : default;

        public LLVMTypeRef LabelType => (this.Handle != IntPtr.Zero) ? LLVM.LabelTypeInContext(this) : default;

        public LLVMTypeRef PPCFP128Type => (this.Handle != IntPtr.Zero) ? LLVM.PPCFP128TypeInContext(this) : default;

        public LLVMTypeRef VoidType => (this.Handle != IntPtr.Zero) ? LLVM.VoidTypeInContext(this) : default;

        public LLVMTypeRef X86FP80Type => (this.Handle != IntPtr.Zero) ? LLVM.X86FP80TypeInContext(this) : default;

        public LLVMTypeRef X86MMXType => (this.Handle != IntPtr.Zero) ? LLVM.X86MMXTypeInContext(this) : default;

        public LLVMTypeRef X86AMXType => (this.Handle != IntPtr.Zero) ? LLVM.X86AMXTypeInContext(this) : default;

        public static implicit operator LLVMContextRef(LLVMOpaqueContext* value) => new LLVMContextRef((IntPtr)value);

        public static implicit operator LLVMOpaqueContext*(LLVMContextRef value) => (LLVMOpaqueContext*)value.Handle;

        public static bool operator ==(LLVMContextRef left, LLVMContextRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMContextRef left, LLVMContextRef right) => !(left == right);

        public static LLVMContextRef Create() => LLVM.ContextCreate();

        public LLVMBasicBlockRef AppendBasicBlock(LLVMValueRef fn, string name) => this.AppendBasicBlock(fn, name.AsSpan());

        public LLVMBasicBlockRef AppendBasicBlock(LLVMValueRef fn, ReadOnlySpan<char> name) => LLVMBasicBlockRef.AppendInContext(this, fn, name);

        public LLVMBasicBlockRef CreateBasicBlock(string name) => this.CreateBasicBlock(name.AsSpan());

        public LLVMBasicBlockRef CreateBasicBlock(ReadOnlySpan<char> name) => LLVMBasicBlockRef.CreateInContext(this, name);

        public LLVMBuilderRef CreateBuilder() => LLVMBuilderRef.Create(this);

        public LLVMMetadataRef CreateDebugLocation(uint line, uint column, LLVMMetadataRef scope, LLVMMetadataRef inlinedAt) => LLVM.DIBuilderCreateDebugLocation(this, line, column, scope, inlinedAt);

        public LLVMModuleRef CreateModuleWithName(string moduleID) => this.CreateModuleWithName(moduleID.AsSpan());

        public LLVMModuleRef CreateModuleWithName(ReadOnlySpan<char> moduleID)
        {
            using var marshaledModuleID = new MarshaledString(moduleID);
            return LLVM.ModuleCreateWithNameInContext(marshaledModuleID, this);
        }

        public LLVMTypeRef CreateNamedStruct(string name) => this.CreateNamedStruct(name.AsSpan());

        public LLVMTypeRef CreateNamedStruct(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.StructCreateNamed(this, marshaledName);
        }

        public void Dispose()
        {
            if (this.Handle != IntPtr.Zero)
            {
                LLVM.ContextDispose(this);
                this.Handle = IntPtr.Zero;
            }
        }

        public override bool Equals(object obj) => (obj is LLVMContextRef other) && this.Equals(other);

        public bool Equals(LLVMContextRef other) => this == other;

        public LLVMValueRef GetConstString(string str, bool dontNullTerminate) => this.GetConstString(str.AsSpan(), dontNullTerminate);

        public LLVMValueRef GetConstString(ReadOnlySpan<char> str, bool dontNullTerminate)
        {
            using var marshaledStr = new MarshaledString(str);
            return LLVM.ConstStringInContext(this, marshaledStr, (uint)marshaledStr.Length, dontNullTerminate ? 1 : 0);
        }

        public LLVMValueRef GetConstStruct(LLVMValueRef[] constantVals, bool packed) => this.GetConstStruct(constantVals.AsSpan(), packed);

        public LLVMValueRef GetConstStruct(ReadOnlySpan<LLVMValueRef> constantVals, bool packed)
        {
            fixed (LLVMValueRef* pConstantVals = constantVals)
            {
                return LLVM.ConstStructInContext(this, (LLVMOpaqueValue**)pConstantVals, (uint)constantVals.Length, packed ? 1 : 0);
            }
        }

        public override int GetHashCode() => this.Handle.GetHashCode();

        public LLVMTypeRef GetIntPtrType(LLVMTargetDataRef tD) => LLVM.IntPtrTypeInContext(this, tD);

        public LLVMTypeRef GetIntPtrTypeForAS(LLVMTargetDataRef tD, uint aS) => LLVM.IntPtrTypeForASInContext(this, tD, aS);

        public LLVMTypeRef GetIntType(uint numBits) => LLVM.IntTypeInContext(this, numBits);

        public uint GetMDKindID(string name, uint sLen) => this.GetMDKindID(name.AsSpan(0, (int)sLen));

        public uint GetMDKindID(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.GetMDKindIDInContext(this, marshaledName, (uint)marshaledName.Length);
        }

        public LLVMValueRef GetMDNode(LLVMValueRef[] vals) => this.GetMDNode(vals.AsSpan());

        public LLVMValueRef GetMDNode(ReadOnlySpan<LLVMValueRef> vals)
        {
            fixed (LLVMValueRef* pVals = vals)
            {
                return LLVM.MDNodeInContext(this, (LLVMOpaqueValue**)pVals, (uint)vals.Length);
            }
        }

        public LLVMValueRef GetMDString(string str, uint sLen) => this.GetMDString(str.AsSpan(0, (int)sLen));

        public LLVMValueRef GetMDString(ReadOnlySpan<char> str)
        {
            using var marshaledStr = new MarshaledString(str);
            return LLVM.MDStringInContext(this, marshaledStr, (uint)marshaledStr.Length);
        }

        public LLVMTypeRef GetStructType(LLVMTypeRef[] elementTypes, bool packed) => this.GetStructType(elementTypes.AsSpan(), packed);

        public LLVMTypeRef GetStructType(ReadOnlySpan<LLVMTypeRef> elementTypes, bool packed)
        {
            fixed (LLVMTypeRef* pElementTypes = elementTypes)
            {
                return LLVM.StructTypeInContext(this, (LLVMOpaqueType**)pElementTypes, (uint)elementTypes.Length, packed ? 1 : 0);
            }
        }

        public LLVMBasicBlockRef InsertBasicBlock(LLVMBasicBlockRef bB, string name) => LLVMBasicBlockRef.InsertInContext(this, bB, name);

        public LLVMValueRef MetadataAsValue(LLVMMetadataRef mD) => LLVM.MetadataAsValue(this, mD);

        public void SetDiagnosticHandler(LLVMDiagnosticHandler handler, IntPtr diagnosticContext)
        {
            var pHandler = Marshal.GetFunctionPointerForDelegate(handler);
            LLVM.ContextSetDiagnosticHandler(this, pHandler, (void*)diagnosticContext);
        }

        public void SetYieldCallback(LLVMYieldCallback callback, IntPtr opaqueHandle)
        {
            var pCallback = Marshal.GetFunctionPointerForDelegate(callback);
            LLVM.ContextSetYieldCallback(this, pCallback, (void*)opaqueHandle);
        }

        public override string ToString() => $"{nameof(LLVMContextRef)}: {this.Handle:X}";

        public bool TryParseBitcode(LLVMMemoryBufferRef memBuf, out LLVMModuleRef outModule, out string outMessage)
        {
            fixed (LLVMModuleRef* pOutModule = &outModule)
            {
                sbyte* pMessage = null;
                var result = LLVM.ParseBitcodeInContext(this, memBuf, (LLVMOpaqueModule**)pOutModule, &pMessage);

                if (pMessage == null)
                {
                    outMessage = string.Empty;
                }
                else
                {
                    var span = new ReadOnlySpan<byte>(pMessage, int.MaxValue);
                    outMessage = span[..span.IndexOf((byte)'\0')].AsString();
                }

                return result == 0;
            }
        }
    }
}
