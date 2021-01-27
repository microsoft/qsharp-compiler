// -----------------------------------------------------------------------
// <copyright file="AtomicRMW.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Atomic Read-Modify-Write operation</summary>
    public enum AtomicRMWBinOp
    {
        /// <summary>Exchange operation</summary>
        Xchg = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpXchg,

        /// <summary>Integer addition operation</summary>
        Add = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAdd,

        /// <summary>Integer subtraction</summary>
        Sub = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpSub,

        /// <summary>Bitwise AND</summary>
        And = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpAnd,

        /// <summary>Bitwise NAND</summary>
        Nand = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpNand,

        /// <summary>Bitwise OR</summary>
        Or = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpOr,

        /// <summary>Bitwise XOR</summary>
        Xor = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpXor,

        /// <summary>Max</summary>
        Max = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpMax,

        /// <summary>Min</summary>
        Min = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpMin,

        /// <summary>Unsigned Max</summary>
        UMax = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpUMax,

        /// <summary>Unsigned Min</summary>
        UMin = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpUMin,

        /// <summary>Floating point addition</summary>
        FAdd = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpFAdd,

        /// <summary>Floating point subtraction</summary>
        FSub = LLVMAtomicRMWBinOp.LLVMAtomicRMWBinOpFSub
    }

    /// <summary>Atomic Read-Modify-Write instruction</summary>
    /// <seealso href="xref:llvm_langref#atomicrmw-instruction">LLVM atomicrmw instruction</seealso>
    public class AtomicRMW
            : Instruction
    {
        /// <summary>Gets or sets the kind of atomic operation for this instruction</summary>
        public unsafe AtomicRMWBinOp Kind
        {
            get => ( AtomicRMWBinOp )ValueHandle.AtomicRMWBinOp;
            set => LLVM.SetAtomicRMWBinOp( ValueHandle, ( LLVMAtomicRMWBinOp )value );
        }

        internal AtomicRMW( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
