// -----------------------------------------------------------------------
// <copyright file="MetadataOperandList.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Ubiquity.ArgValidators;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

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
        public LlvmMetadata? this[ int index ]
        {
            get => GetOperand<LlvmMetadata>( index );
            set
            {
                index.ValidateRange( 0, Count - 1, nameof( index ) );
                LibLLVMMDNodeReplaceOperand( Container.MetadataHandle, ( uint )index, value?.MetadataHandle ?? default );
            }
        }

        /// <summary>Gets the count of operands in this collection</summary>
        public int Count => checked(( int )LibLLVMMDNodeGetNumOperands( Container.MetadataHandle ));

        /// <summary>Gets an enumerator for the operands in this collection</summary>
        /// <returns>Enumerator of operands</returns>
        public IEnumerator<LlvmMetadata?> GetEnumerator( )
        {
            for( int i = 0; i < Count; ++i )
            {
                yield return GetOperand<LlvmMetadata>( i );
            }
        }

        /// <summary>Gets an enumerator for the operands in this collection</summary>
        /// <returns>Enumerator of operands</returns>
        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        /// <inheritdoc/>
        public bool Contains( LlvmMetadata? item ) => this.Any( n => n == item );

        /// <summary>Specialized indexer to get the element as a specific derived type</summary>
        /// <typeparam name="TItem">Type of the element (must be derived from <see cref="LlvmMetadata"/></typeparam>
        /// <param name="i">index for the item</param>
        /// <returns>Item at the specified index</returns>
        /// <exception cref="ArgumentOutOfRangeException">index is out of range for the collection</exception>
        /// <exception cref="InvalidCastException">If the element at the index is not castable to <typeparamref name="TItem"/></exception>
        /// <remarks>This provides a common (and likely internally optimized) means of getting an element as a specific type</remarks>
        public TItem? GetOperand<TItem>( Index i )
            where TItem : LlvmMetadata
        {
            uint offset = ( uint )i.GetOffset(Count);
            offset.ValidateRange( 0u, ( uint )Count, nameof( i ) );
            var node = LibLLVMGetOperandNode( LibLLVMMDNodeGetOperand( Container.MetadataHandle, offset ) );
            return LlvmMetadata.FromHandle<TItem>( Container.Context, node );
        }

        internal MetadataOperandCollection( MDNode container )
        {
            Container = container;
        }

        private readonly MDNode Container;
    }
}
