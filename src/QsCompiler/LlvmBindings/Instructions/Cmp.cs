// -----------------------------------------------------------------------
// <copyright file="Cmp.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Base class for compare instructions.</summary>
    public class Cmp
        : Instruction
    {
        /// <summary>Gets the predicate for the comparison.</summary>
        public unsafe Predicate Predicate => this.Opcode switch
        {
            OpCode.ICmp => (Predicate)LLVM.GetICmpPredicate(this.ValueHandle),
            OpCode.FCmp => (Predicate)LLVM.GetFCmpPredicate(this.ValueHandle),
            _ => Predicate.BadFcmpPredicate,
        };

        /* TODO: Predicate {set;} */

        internal Cmp(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }
    }
}
