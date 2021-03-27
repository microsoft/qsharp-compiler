// -----------------------------------------------------------------------
// <copyright file="BasicBlockCollection.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm.Values
{
    internal class BasicBlockCollection
        : ICollection<BasicBlock>
    {
        private readonly IrFunction ContainingFunction;

        internal BasicBlockCollection(IrFunction function)
        {
            this.ContainingFunction = function;
        }

        public bool IsReadOnly => false;

        /// <summary>Gets a count of the blocks in the collection.</summary>
        public int Count => checked((int)this.ContainingFunction.ValueHandle.BasicBlocksCount);

        /// <summary>Add a block to the underlying function.</summary>
        /// <param name="item"><see cref="BasicBlock"/> to add to the function.</param>
        /// <remarks>
        /// The block is appended to the end of the list of blocks owned by the function.
        /// </remarks>
        public unsafe void Add(BasicBlock item)
        {
            if (item.ContainingFunction == default)
            {
                LLVM.AppendExistingBasicBlock(this.ContainingFunction.ValueHandle, item.BlockHandle);
            }

            if (item.ContainingFunction != this.ContainingFunction)
            {
                throw new ArgumentException();
            }

            throw new ArgumentException();
        }

        /// <inheritdoc/>
        public IEnumerator<BasicBlock> GetEnumerator()
        {
            LLVMBasicBlockRef blockRef = this.ContainingFunction.ValueHandle.FirstBasicBlock;
            while (blockRef != default)
            {
                yield return BasicBlock.FromHandle(blockRef)!;
                blockRef = blockRef.Next;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public void Clear()
        {
            var items = this.ToList();
            foreach (var bb in items)
            {
                this.Remove(bb);
            }
        }

        public bool Contains(BasicBlock item)
        {
            return item.ContainingFunction == this.ContainingFunction;
        }

        public void CopyTo(BasicBlock[] array, int arrayIndex)
        {
            if (this.Count + arrayIndex > array.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            foreach (var block in this)
            {
                array[arrayIndex++] = block;
            }
        }

        public bool Remove(BasicBlock item)
        {
            if (item.ContainingFunction != this.ContainingFunction)
            {
                return false;
            }

            item.BlockHandle.RemoveFromParent();
            return true;
        }
    }
}
