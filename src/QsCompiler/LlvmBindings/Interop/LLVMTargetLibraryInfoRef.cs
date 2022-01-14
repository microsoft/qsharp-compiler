// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMTargetLibraryInfoRef : IEquatable<LLVMTargetLibraryInfoRef>
    {
        public IntPtr Handle;

        public LLVMTargetLibraryInfoRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMTargetLibraryInfoRef(LLVMOpaqueTargetLibraryInfotData* value) => new LLVMTargetLibraryInfoRef((IntPtr)value);

        public static implicit operator LLVMOpaqueTargetLibraryInfotData*(LLVMTargetLibraryInfoRef value) => (LLVMOpaqueTargetLibraryInfotData*)value.Handle;

        public static bool operator ==(LLVMTargetLibraryInfoRef left, LLVMTargetLibraryInfoRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMTargetLibraryInfoRef left, LLVMTargetLibraryInfoRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMTargetLibraryInfoRef other) && this.Equals(other);

        public bool Equals(LLVMTargetLibraryInfoRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMTargetLibraryInfoRef)}: {this.Handle:X}";
    }
}
