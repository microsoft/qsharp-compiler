// -----------------------------------------------------------------------
// <copyright file="SuccessorBlockCollection.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm.Instructions
{
    /// <summary>Support class to provide read/update semantics for the successor blocks of an instruction.</summary>
    /// <remarks>
    /// This class is used to implement Operand lists of elements including sub lists based on an offset.
    /// The latter case is useful for types that expose some fixed set of operands as properties and some
    /// arbitrary number of additional items as operands.
    /// </remarks>
    public sealed class SuccessorBlockCollection
        : IOperandCollection<BasicBlock>
    {
        private readonly Instruction container;

        internal SuccessorBlockCollection(Instruction container)
        {
            this.container = container;
        }

        /// <summary>Gets the count of elements in this collection.</summary>
        public int Count => checked((int)this.container.ValueHandle.SuccessorsCount);

        /// <inheritdoc/>
        public BasicBlock this[int index]
        {
            get
            {
                return BasicBlock.FromHandle(this.container.ValueHandle.GetSuccessor((uint)index))!;
            }

            set
            {
                this.container.ValueHandle.SetSuccessor((uint)index, value?.BlockHandle ?? default);
            }
        }

        /// <summary>Gets an enumerator for the <see cref="BasicBlock"/>s in this collection.</summary>
        /// <returns>Enumerator for the collection.</returns>
        public IEnumerator<BasicBlock> GetEnumerator()
        {
            for (int i = 0; i < this.Count; ++i)
            {
                yield return BasicBlock.FromHandle(this.container.ValueHandle.GetSuccessor((uint)i))!;
            }
        }

        /// <summary>Gets an enumerator for the <see cref="BasicBlock"/>s in this collection.</summary>
        /// <returns>Enumerator for the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        /// <summary>Determines whether a <see cref="BasicBlock"/> is in this collection.</summary>
        /// <returns>true if item is found in the collaction; otherwise, false.</returns>
        public bool Contains(BasicBlock item) => this.Any(n => n == item);
    }
}
