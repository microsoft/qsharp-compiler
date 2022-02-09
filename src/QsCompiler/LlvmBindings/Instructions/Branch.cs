// -----------------------------------------------------------------------
// <copyright file="Branch.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using LLVMSharp.Interop;

using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Branch instruction.</summary>
    public class Branch
        : Terminator
    {
        internal Branch(LLVMValueRef valueRef)
            : base(valueRef)
        {
        }

        /// <summary>Gets a value indicating whether this branch is conditional.</summary>
        public bool IsConditional => this.ValueHandle.IsConditional;

        /// <summary>Gets the condition for the branch, if any.</summary>
        public Value? Condition
            => !this.IsConditional ? default : FromHandle<Value>(this.ValueHandle.Condition);
    }
}
