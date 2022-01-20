// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMMetadataRef : IEquatable<LLVMMetadataRef>
    {
        public IntPtr Handle;

        public LLVMMetadataRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMMetadataRef(LLVMOpaqueMetadata* value) => new LLVMMetadataRef((IntPtr)value);

        public static implicit operator LLVMOpaqueMetadata*(LLVMMetadataRef value) => (LLVMOpaqueMetadata*)value.Handle;

        public static bool operator ==(LLVMMetadataRef left, LLVMMetadataRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMMetadataRef left, LLVMMetadataRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMMetadataRef other) && this.Equals(other);

        public bool Equals(LLVMMetadataRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMMetadataRef)}: {this.Handle:X}";
    }
}
