// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMComdatRef : IEquatable<LLVMComdatRef>
    {
        public IntPtr Handle;

        public LLVMComdatRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMComdatRef(LLVMComdat* comdat) => new LLVMComdatRef((IntPtr)comdat);

        public static implicit operator LLVMComdat*(LLVMComdatRef comdat) => (LLVMComdat*)comdat.Handle;

        public static bool operator ==(LLVMComdatRef left, LLVMComdatRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMComdatRef left, LLVMComdatRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMComdatRef other) && this.Equals(other);

        public bool Equals(LLVMComdatRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMComdatRef)}: {this.Handle:X}";
    }
}
