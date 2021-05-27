// -----------------------------------------------------------------------
// <copyright file="MetadataOperandCollection.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LLVMSharp.Interop;
using Ubiquity.NET.Llvm.Values;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Support class to provide read/update semantics to the operands of a container element</summary>
    /// <remarks>
    /// This class is used to implement Operand lists of elements including sub lists based on an offset.
    /// The latter case is useful for types that expose some fixed set of operands as properties and some
    /// arbitrary number of additional items as operands.
    /// </remarks>
    public sealed class MetadataOperandCollection
        : IOperandCollection<LlvmMetadata?>
    {
        /// <inheritdoc/>
        public LlvmMetadata? this[int index]
        {
            get => this.GetOperand<LlvmMetadata>(index);
        }

        /// <summary>Gets the count of operands in this collection</summary>
        public int Count
        {
            get
            {
                var valueHandle = this.container.Context.ContextHandle.MetadataAsValue(this.container.MetadataHandle);
                return checked((int)valueHandle.GetMDNodeNumOperands());
            }
        }

        /// <summary>Gets an enumerator for the operands in this collection</summary>
        /// <returns>Enumerator of operands</returns>
        public IEnumerator<LlvmMetadata?> GetEnumerator()
        {
            for (int i = 0; i < this.Count; ++i)
            {
                yield return this.GetOperand<LlvmMetadata>(i);
            }
        }

        /// <summary>Gets an enumerator for the operands in this collection</summary>
        /// <returns>Enumerator of operands</returns>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public bool Contains(LlvmMetadata? item) => this.Any(n => n == item);

        /// <summary>Specialized indexer to get the element as a specific derived type</summary>
        /// <typeparam name="TItem">Type of the element (must be derived from <see cref="LlvmMetadata"/></typeparam>
        /// <param name="i">index for the item</param>
        /// <returns>Item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range for the collection</exception>
        /// <exception cref="InvalidCastException">If the element at the index is not castable to <typeparamref name="TItem"/></exception>
        public TItem? GetOperand<TItem>(Index i)
            where TItem : LlvmMetadata
        {
            var operand = this.GetOperandValue(i);
            if (operand == null)
            {
                return null;
            }

            var node = operand.ValueHandle.ValueAsMetadata();
            return LlvmMetadata.FromHandle<TItem>(this.container.Context, node);
        }

        /// <summary>Indexer to get the element as a <see cref="Value"/>.</summary>
        /// <param name="i">index for the item</param>
        /// <returns>Item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range for the collection</exception>
        public Value? GetOperandValue(Index i)
        {
            var offset = i.GetOffset(this.Count);
            var valueHandle = this.container.Context.ContextHandle.MetadataAsValue(this.container.MetadataHandle);
            var operands = valueHandle.GetMDNodeOperands();

            if (offset >= operands.Length)
            {
                throw new ArgumentOutOfRangeException($"No operand exists at offset {offset}.");
            }

            var operand = operands[offset];
            if (operand == default)
            {
                // Requested operand is nullptr.
                return null;
            }

            return Value.FromHandle(operand);
        }

        internal MetadataOperandCollection(MDNode container)
        {
            this.container = container;
        }

        private readonly MDNode container;
    }
}
