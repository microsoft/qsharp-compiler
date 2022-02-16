// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMRelocationIteratorRef : IEquatable<LLVMRelocationIteratorRef>
    {
        public IntPtr Handle;

        public LLVMRelocationIteratorRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMRelocationIteratorRef(LLVMOpaqueRelocationIterator* value) => new LLVMRelocationIteratorRef((IntPtr)value);

        public static implicit operator LLVMOpaqueRelocationIterator*(LLVMRelocationIteratorRef value) => (LLVMOpaqueRelocationIterator*)value.Handle;

        public static bool operator ==(LLVMRelocationIteratorRef left, LLVMRelocationIteratorRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMRelocationIteratorRef left, LLVMRelocationIteratorRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMRelocationIteratorRef other) && this.Equals(other);

        public bool Equals(LLVMRelocationIteratorRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMRelocationIteratorRef)}: {this.Handle:X}";
    }
}
