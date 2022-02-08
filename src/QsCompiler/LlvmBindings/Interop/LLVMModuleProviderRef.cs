// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMModuleProviderRef : IEquatable<LLVMModuleProviderRef>
    {
        public IntPtr Handle;

        public LLVMModuleProviderRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMModuleProviderRef(LLVMOpaqueModuleProvider* value) => new LLVMModuleProviderRef((IntPtr)value);

        public static implicit operator LLVMOpaqueModuleProvider*(LLVMModuleProviderRef value) => (LLVMOpaqueModuleProvider*)value.Handle;

        public static bool operator ==(LLVMModuleProviderRef left, LLVMModuleProviderRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMModuleProviderRef left, LLVMModuleProviderRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMModuleProviderRef other) && this.Equals(other);

        public bool Equals(LLVMModuleProviderRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMModuleProviderRef)}: {this.Handle:X}";
    }
}
