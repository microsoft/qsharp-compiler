// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMSymbolIteratorRef : IEquatable<LLVMSymbolIteratorRef>
    {
        public IntPtr Handle;

        public LLVMSymbolIteratorRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMSymbolIteratorRef(LLVMOpaqueSymbolIterator* value) => new LLVMSymbolIteratorRef((IntPtr)value);

        public static implicit operator LLVMOpaqueSymbolIterator*(LLVMSymbolIteratorRef value) => (LLVMOpaqueSymbolIterator*)value.Handle;

        public static bool operator ==(LLVMSymbolIteratorRef left, LLVMSymbolIteratorRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMSymbolIteratorRef left, LLVMSymbolIteratorRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMSymbolIteratorRef other) && this.Equals(other);

        public bool Equals(LLVMSymbolIteratorRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMSymbolIteratorRef)}: {this.Handle:X}";
    }
}
