// -----------------------------------------------------------------------
// <copyright file="TupleTypedArrayWrapper.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ubiquity.NET.Llvm.DebugInfo
{
    /// <summary>Generic wrapper to treat an MDTuple as an array of elements of specific type</summary>
    /// <typeparam name="T">Type of elements</typeparam>
    /// <remarks>
    /// This implements a facade pattern that presents an <see cref="IReadOnlyCollection{T}"/> for the
    /// operands of an <see cref="MDTuple"/>. This allows treating the tuple like an array of nodes of a
    /// particular type.
    /// </remarks>
    [SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Collection doesn't make sense for this type")]
    public class TupleTypedArrayWrapper<T>
        : IReadOnlyList<T?>
        where T : LlvmMetadata
    {
        /// <summary>Gets the underlying tuple for this wrapper</summary>
        public MDTuple? Tuple { get; }

        /// <summary>Gets the count of operands in the <see cref="MDTuple"/></summary>
        public int Count => this.Tuple?.Operands.Count ?? 0;

        /// <summary>Gets an item from the tuple</summary>
        /// <param name="index">Index of the item to retrieve</param>
        /// <returns>The element at <paramref name="index"/> in the tuple</returns>
        public T? this[int index]
        {
            get
            {
                return this.Tuple!.Operands.GetOperand<T>(index);
            }
        }

        /// <summary>Gets an enumerator for the items in the <see cref="MDTuple"/></summary>
        /// <returns>Enumerator</returns>
        /// <remarks>If the underlying tuple is empty this is an empty enumeration</remarks>
        public IEnumerator<T?> GetEnumerator()
        {
            return this.Tuple is null
                ? Enumerable.Empty<T>().GetEnumerator()
                : this.Tuple.Operands
                        .Cast<T>()
                        .GetEnumerator();
        }

        /// <summary>Gets an enumerator for the items in the <see cref="MDTuple"/></summary>
        /// <returns>Enumerator</returns>
        /// <remarks>If the underlying tuple is empty this is an empty enumeration</remarks>
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        internal TupleTypedArrayWrapper(MDTuple? tuple)
        {
            this.Tuple = tuple;
        }
    }
}
