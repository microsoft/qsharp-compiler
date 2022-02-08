// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMUseRef : IEquatable<LLVMUseRef>
    {
        public IntPtr Handle;

        public LLVMUseRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMUseRef(LLVMOpaqueUse* use) => new LLVMUseRef((IntPtr)use);

        public static implicit operator LLVMOpaqueUse*(LLVMUseRef use) => (LLVMOpaqueUse*)use.Handle;

        public static bool operator ==(LLVMUseRef left, LLVMUseRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMUseRef left, LLVMUseRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMUseRef other) && this.Equals(other);

        public bool Equals(LLVMUseRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMUseRef)}: {this.Handle:X}";
    }
}
