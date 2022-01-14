// Copyright (c) .NET Foundation and Contributors. All Rights Reserved. Licensed under the MIT License (MIT).

using System;

namespace LlvmBindings.Interop
{
    public unsafe partial struct LLVMBasicBlockRef : IEquatable<LLVMBasicBlockRef>
    {
        public IntPtr Handle;

        public LLVMBasicBlockRef(IntPtr handle)
        {
            this.Handle = handle;
        }

        public LLVMValueRef FirstInstruction => (this.Handle != IntPtr.Zero) ? LLVM.GetFirstInstruction(this) : default;

        public LLVMValueRef LastInstruction => (this.Handle != IntPtr.Zero) ? LLVM.GetLastInstruction(this) : default;

        public LLVMBasicBlockRef Next => (this.Handle != IntPtr.Zero) ? LLVM.GetNextBasicBlock(this) : default;

        public LLVMValueRef Parent => (this.Handle != IntPtr.Zero) ? LLVM.GetBasicBlockParent(this) : default;

        public LLVMBasicBlockRef Previous => (this.Handle != IntPtr.Zero) ? LLVM.GetPreviousBasicBlock(this) : default;

        public LLVMValueRef Terminator => (this.Handle != IntPtr.Zero) ? LLVM.GetBasicBlockTerminator(this) : default;

        public static explicit operator LLVMBasicBlockRef(LLVMOpaqueValue* value) => new LLVMBasicBlockRef((IntPtr)value);

        public static implicit operator LLVMBasicBlockRef(LLVMOpaqueBasicBlock* value) => new LLVMBasicBlockRef((IntPtr)value);

        public static implicit operator LLVMOpaqueBasicBlock*(LLVMBasicBlockRef value) => (LLVMOpaqueBasicBlock*)value.Handle;

        public static implicit operator LLVMOpaqueValue*(LLVMBasicBlockRef value) => (LLVMOpaqueValue*)value.Handle;

        public static bool operator ==(LLVMBasicBlockRef left, LLVMBasicBlockRef right) => left.Handle == right.Handle;

        public static bool operator !=(LLVMBasicBlockRef left, LLVMBasicBlockRef right) => !(left == right);

        public static LLVMBasicBlockRef AppendInContext(LLVMContextRef c, LLVMValueRef fn, string name) => AppendInContext(c, fn, name.AsSpan());

        public static LLVMBasicBlockRef AppendInContext(LLVMContextRef c, LLVMValueRef fn, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.AppendBasicBlockInContext(c, fn, marshaledName);
        }

        public static LLVMBasicBlockRef CreateInContext(LLVMContextRef c, string name) => CreateInContext(c, name.AsSpan());

        public static LLVMBasicBlockRef CreateInContext(LLVMContextRef c, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.CreateBasicBlockInContext(c, marshaledName);
        }

        public static LLVMBasicBlockRef InsertInContext(LLVMContextRef c, LLVMBasicBlockRef bB, string name) => InsertInContext(c, bB, name.AsSpan());

        public static LLVMBasicBlockRef InsertInContext(LLVMContextRef c, LLVMBasicBlockRef bB, ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.InsertBasicBlockInContext(c, bB, marshaledName);
        }

        public LLVMValueRef AsValue() => LLVM.BasicBlockAsValue(this);

        public void Delete() => LLVM.DeleteBasicBlock(this);

        public void Dump() => LLVM.DumpValue(this);

        public override bool Equals(object obj) => (obj is LLVMBasicBlockRef other) && this.Equals(other);

        public bool Equals(LLVMBasicBlockRef other) => this == other;

        public override int GetHashCode() => this.Handle.GetHashCode();

        public LLVMBasicBlockRef InsertBasicBlock(string name) => this.InsertBasicBlock(name.AsSpan());

        public LLVMBasicBlockRef InsertBasicBlock(ReadOnlySpan<char> name)
        {
            using var marshaledName = new MarshaledString(name);
            return LLVM.InsertBasicBlock(this, marshaledName);
        }

        public void MoveAfter(LLVMBasicBlockRef movePos) => LLVM.MoveBasicBlockAfter(this, movePos);

        public void MoveBefore(LLVMBasicBlockRef movePos) => LLVM.MoveBasicBlockBefore(this, movePos);

        public string PrintToString()
        {
            var pStr = LLVM.PrintValueToString(this);

            if (pStr == null)
            {
                return string.Empty;
            }

            var span = new ReadOnlySpan<byte>(pStr, int.MaxValue);

            var result = span.Slice(0, span.IndexOf((byte)'\0')).AsString();
            LLVM.DisposeMessage(pStr);
            return result;
        }

        public void RemoveFromParent() => LLVM.RemoveBasicBlockFromParent(this);

        public override string ToString() => (this.Handle != IntPtr.Zero) ? this.PrintToString() : string.Empty;
    }
}
