// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMMemoryBufferRef : IEquatable<LLVMMemoryBufferRef>
    {
        public IntPtr Handle;

        public LLVMMemoryBufferRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMMemoryBufferRef(LLVMOpaqueMemoryBuffer* memoryBuffer) => new LLVMMemoryBufferRef((IntPtr)memoryBuffer);

        public static implicit operator LLVMOpaqueMemoryBuffer*(LLVMMemoryBufferRef memoryBuffer) => (LLVMOpaqueMemoryBuffer*)memoryBuffer.Handle;

        public static bool operator ==(LLVMMemoryBufferRef left, LLVMMemoryBufferRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMMemoryBufferRef left, LLVMMemoryBufferRef right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMMemoryBufferRef other) && this.Equals(other);

        public bool Equals(LLVMMemoryBufferRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMMemoryBufferRef)}: {this.Handle:X}";
    }
}
