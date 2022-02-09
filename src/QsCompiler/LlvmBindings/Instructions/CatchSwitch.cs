// -----------------------------------------------------------------------
// <copyright file="CatchSwitch.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Describes the set of possible catch handlers that may be executed by an
    /// <see href="xref:llvm_langref#personalityfn">EH personality routine</see>.</summary>
    /// <seealso href="xref:llvm_langref#i-catchswitch">LLVM catchswitch instruction</seealso>
    /// <seealso href="xref:llvm_exception_handling#exception-handling-in-llvm">Exception Handling in LLVM</seealso>
    /// <seealso href="xref:llvm_exception_handling#wineh">Exception Handling using the Windows Runtime</seealso>
    public class CatchSwitch
        : Instruction
    {
        internal CatchSwitch(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        /// <summary>Gets or sets the Parent pad for this <see cref="CatchSwitch"/>.</summary>
        public Value ParentPad
        {
            get => this.Operands.GetOperand<Value>(0)!;
            set => this.Operands[0] = value;
        }

        /// <summary>Gets or sets the Unwind destination for this <see cref="CatchSwitch"/>.</summary>
        public unsafe BasicBlock? UnwindDestination
        {
            get
            {
                var handle = LLVM.GetUnwindDest(this.ValueHandle);
                return handle == default ? default : BasicBlock.FromHandle(handle);
            }

            set
            {
                LLVM.SetUnwindDest(this.ValueHandle, value!.BlockHandle);
            }
        }
    }
}
