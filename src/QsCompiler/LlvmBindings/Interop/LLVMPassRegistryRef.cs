// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMPassRegistryRef : IEquatable<LLVMPassRegistryRef>
    {
        public IntPtr Handle;

        public LLVMPassRegistryRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMPassRegistryRef(LLVMOpaquePassRegistry* value) => new LLVMPassRegistryRef((IntPtr)value);

        public static implicit operator LLVMOpaquePassRegistry*(LLVMPassRegistryRef value) => (LLVMOpaquePassRegistry*)value.Handle;

        public static bool operator ==(LLVMPassRegistryRef left, LLVMPassRegistryRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMPassRegistryRef left, LLVMPassRegistryRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMPassRegistryRef other) && this.Equals(other);

        public bool Equals(LLVMPassRegistryRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMPassRegistryRef)}: {this.Handle:X}";
    }
}
