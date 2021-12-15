// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMObjectFileRef : IEquatable<LLVMObjectFileRef>
    {
        public IntPtr Handle;

        public LLVMObjectFileRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMObjectFileRef(LLVMOpaqueObjectFile* value) => new LLVMObjectFileRef((IntPtr)value);

        public static implicit operator LLVMOpaqueObjectFile*(LLVMObjectFileRef value) => (LLVMOpaqueObjectFile*)value.Handle;

        public static bool operator ==(LLVMObjectFileRef left, LLVMObjectFileRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMObjectFileRef left, LLVMObjectFileRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMObjectFileRef other) && this.Equals(other);

        public bool Equals(LLVMObjectFileRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMObjectFileRef)}: {this.Handle:X}";
    }
}
