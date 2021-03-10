// -----------------------------------------------------------------------
// <copyright company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Support class to provide read/update semantics to the operands of a container element</summary>
    /// <typeparam name="T">Type of elements in the collection</typeparam>
    /// <remarks>
    /// This class is used to implement Operand lists of elements including sub lists based on an offset.
    /// The latter case is useful for types that expose some fixed set of operands as properties and some
    /// arbitrary number of additional items as operands.
    /// </remarks>
    public sealed class ValueOperandListCollection<T>
        : IOperandCollection<T>
        where T : Value
    {
        /// <summary>Gets the operand at the specified index</summary>
        /// <param name="index">Index of the operand to receive</param>
        /// <returns>Item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range for this collection</exception>
        public T this[ int index ]
        {
            get => GetOperand<T>( index );
            set
            {
                Container.ValueHandle.SetOperand( ( uint )index, value?.ValueHandle ?? default );
            }
        }

        /// <summary>Gets the count of operands in this collection</summary>
        public int Count => Container.ValueHandle.OperandCount;

        /// <summary>Gets an enumerator for this collection</summary>
        /// <returns>Enumerator for the operands in this collection</returns>
        public IEnumerator<T> GetEnumerator( )
        {
            for( int i = 0; i < Count; ++i )
            {
                var element = GetOperand<T>( i );
                if( element == default )
                {
                    yield break;
                }

                yield return element;
            }
        }

        /// <summary>Gets an enumerator for this collection</summary>
        /// <returns>Enumerator for the operands in this collection</returns>
        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        /// <inheritdoc/>
        public bool Contains( T item ) => this.Any( n => n == item );

        /// <summary>Specialized indexer to get the element as a specific derived type</summary>
        /// <typeparam name="TItem">Type of the element (must be derived from <see cref="LlvmMetadata"/></typeparam>
        /// <param name="i">index for the item</param>
        /// <returns>Item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range for the collection</exception>
        /// <exception cref="InvalidCastException">If the element at the index is not castable to <typeparamref name="TItem"/></exception>
        /// <remarks>This provides a common (and likely internally optimized) means of getting an element as a specific type</remarks>
        public TItem GetOperand<TItem>( Index i )
            where TItem : T
        {
            uint offset = ( uint )i.GetOffset(Count);
            return Value.FromHandle<TItem>( Container.ValueHandle.GetOperand( offset ) );
        }

        internal ValueOperandListCollection( Value container )
        {
            Container = container;
        }

        private readonly Value Container;
    }
}
