// -----------------------------------------------------------------------
// <copyright file="User.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Contains an LLVM User value.</summary>
    /// <remarks>
    /// A user is one role in the user->uses relationship
    /// conveyed by the LLVM value model. A User can contain
    /// references (e.g. uses) of other values.
    /// </remarks>
    public class User
        : Value
    {
        /// <summary>Gets a list of the operands for this User.</summary>
        public ValueOperandListCollection<Value> Operands { get; }

        /// <summary>Gets a collection of <see cref="Use"/>s used by this User.</summary>
        public IEnumerable<Use> Uses
        {
            get
            {
                LLVMUseRef current = this.ValueHandle.FirstUse;
                while (current != default)
                {
                    // TODO: intern the use instances?
                    yield return new Use(current);
                    current = current.Next();
                }
            }
        }

        internal User(LLVMValueRef userRef)
            : base(userRef)
        {
            this.Operands = new ValueOperandListCollection<Value>(this);
        }
    }
}
