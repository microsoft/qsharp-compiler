// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMBinaryRef : IEquatable<LLVMBinaryRef>
    {
        public IntPtr Handle;

        public LLVMBinaryRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMBinaryRef(LLVMOpaqueBinary* comdat) => new LLVMBinaryRef((IntPtr)comdat);

        public static implicit operator LLVMOpaqueBinary*(LLVMBinaryRef comdat) => (LLVMOpaqueBinary*)comdat.Handle;

        public static bool operator ==(LLVMBinaryRef left, LLVMBinaryRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMBinaryRef left, LLVMBinaryRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMBinaryRef other) && this.Equals(other);

        public bool Equals(LLVMBinaryRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMBinaryRef)}: {this.Handle:X}";
    }
}
