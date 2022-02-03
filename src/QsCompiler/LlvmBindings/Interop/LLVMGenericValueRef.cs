// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMGenericValueRef : IEquatable<LLVMGenericValueRef>
    {
        public IntPtr Handle;

        public LLVMGenericValueRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMGenericValueRef(LLVMOpaqueGenericValue* genericValue) => new LLVMGenericValueRef((IntPtr)genericValue);

        public static implicit operator LLVMOpaqueGenericValue*(LLVMGenericValueRef genericValue) => (LLVMOpaqueGenericValue*)genericValue.Handle;

        public static bool operator ==(LLVMGenericValueRef left, LLVMGenericValueRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMGenericValueRef left, LLVMGenericValueRef right) => !(left == right);

        public LLVMGenericValueRef CreateInt(LLVMTypeRef ty, ulong n, bool isSigned) => LLVM.CreateGenericValueOfInt(ty, n, isSigned ? 1 : 0);

        public LLVMGenericValueRef CreateFloat(LLVMTypeRef ty, double n) => LLVM.CreateGenericValueOfFloat(ty, n);

        public override bool Equals(object obj) => (obj is LLVMGenericValueRef other) && this.Equals(other);

        public bool Equals(LLVMGenericValueRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMGenericValueRef)}: {this.Handle:X}";
    }
}
