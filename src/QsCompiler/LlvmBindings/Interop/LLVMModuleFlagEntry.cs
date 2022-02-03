// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMModuleFlagEntry : IEquatable<LLVMModuleFlagEntry>
    {
        public IntPtr Handle;

        public LLVMModuleFlagEntry(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMModuleFlagEntry(LLVMOpaqueModuleFlagEntry* comdat) => new LLVMModuleFlagEntry((IntPtr)comdat);

        public static implicit operator LLVMOpaqueModuleFlagEntry*(LLVMModuleFlagEntry comdat) => (LLVMOpaqueModuleFlagEntry*)comdat.Handle;

        public static bool operator ==(LLVMModuleFlagEntry left, LLVMModuleFlagEntry right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMModuleFlagEntry left, LLVMModuleFlagEntry right) => !(left == right);

        public override bool Equals(object obj) => (obj is LLVMModuleFlagEntry other) && this.Equals(other);

        public bool Equals(LLVMModuleFlagEntry other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMModuleFlagEntry)}: {this.Handle:X}";
    }
}
