// -----------------------------------------------------------------------
// <copyright file="LandingPad.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Marks a <see cref="BasicBlock"/> as a catch handler</summary>
    /// <remarks>
    /// Like the <see cref="CatchPad"/>, instruction this must be the first non-phi instruction
    /// in the block.
    /// </remarks>
    /// <seealso href="xref:llvm_langref#i-landingpad">LLVM landing Instruction</seealso>
    /// <seealso href="xref:llvm_exception_handling#exception-handling-in-llvm">Exception Handling in LLVM</seealso>
    public class LandingPad
        : Instruction
    {
        /// <summary>Gets or sets a value indicating whether this <see cref="LandingPad"/> is a cleanup pad</summary>
        public unsafe bool IsCleanup
        {
            get => ValueHandle.IsCleanup;
            set => LLVM.SetCleanup( ValueHandle, value ? 1 : 0 );
        }

        /// <summary>Gets the collection of clauses for this landing pad</summary>
        public ValueOperandListCollection<Constant> Clauses { get; }

        internal LandingPad( LLVMValueRef valueRef )
            : base( valueRef )
        {
            Clauses = new ValueOperandListCollection<Constant>( this );
        }
    }
}
