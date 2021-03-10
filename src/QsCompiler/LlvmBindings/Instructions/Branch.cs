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
    /// <summary>Branch instruction</summary>
    public class Branch
        : Terminator
    {
        /// <summary>Gets a value indicating whether this branch is conditional</summary>
        public bool IsConditional => ValueHandle.IsConditional;

        /// <summary>Gets the condition for the branch, if any</summary>
        public Value Condition
            => !IsConditional ? default : FromHandle<Value>( ValueHandle.Condition );

        internal Branch( LLVMValueRef valueRef )
            : base( valueRef )
        {
        }
    }
}
