// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMTargetMachineRef : IEquatable<LLVMTargetMachineRef>
    {
        public IntPtr Handle;

        public LLVMTargetMachineRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public static implicit operator LLVMTargetMachineRef(LLVMOpaqueTargetMachine* value) => new LLVMTargetMachineRef((IntPtr)value);

        public static implicit operator LLVMOpaqueTargetMachine*(LLVMTargetMachineRef value) => (LLVMOpaqueTargetMachine*)value.Handle;

        public static bool operator ==(LLVMTargetMachineRef left, LLVMTargetMachineRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMTargetMachineRef left, LLVMTargetMachineRef right) => !(left == right);

        public LLVMTargetDataRef CreateTargetDataLayout() => LLVM.CreateTargetDataLayout(this);

        public override bool Equals(object obj) => (obj is LLVMTargetMachineRef other) && this.Equals(other);

        public bool Equals(LLVMTargetMachineRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public override string ToString() => $"{nameof(LLVMTargetMachineRef)}: {this.Handle:X}";
    }
}
