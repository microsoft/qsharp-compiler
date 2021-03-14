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
        /// <summary>Gets a count of the blocks in the collection</summary>
        public int Count => checked( ( int )ContainingFunction.ValueHandle.BasicBlocksCount );

        /// <summary>Add a block to the underlying function</summary>
        /// <param name="item"><see cref="BasicBlock"/> to add to the function</param>
        /// <remarks>
        /// The block is appended to the end of the list of blocks owned by the function
        /// </remarks>
        public unsafe void Add( BasicBlock item )
        {
            if( item.ContainingFunction == default )
            {
                LLVM.AppendExistingBasicBlock( ContainingFunction.ValueHandle, item.BlockHandle );
            }

            if( item.ContainingFunction != ContainingFunction )
            {
                throw new ArgumentException( );
            }

            throw new ArgumentException( );
        }

        /// <inheritdoc/>
        public IEnumerator<BasicBlock> GetEnumerator( )
        {
            LLVMBasicBlockRef blockRef = ContainingFunction.ValueHandle.FirstBasicBlock;
            while( blockRef != default )
            {
                yield return BasicBlock.FromHandle( blockRef )!;
                blockRef = blockRef.Next;
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        public void Clear( )
        {
            var items = this.ToList( );
            foreach( var bb in items )
            {
                Remove( bb );
            }
        }

        public bool Contains( BasicBlock item )
        {
            return item.ContainingFunction == ContainingFunction;
        }

        public void CopyTo( BasicBlock[ ] array, int arrayIndex )
        {
            if( Count + arrayIndex > array.Length )
            {
                throw new ArgumentOutOfRangeException( nameof( arrayIndex ) );
            }

            foreach( var block in this )
            {
                array[ arrayIndex++ ] = block;
            }
        }

        public bool Remove( BasicBlock item )
        {
            if( item.ContainingFunction != ContainingFunction )
            {
                return false;
            }

            item.BlockHandle.RemoveFromParent( );
            return true;
        }

        public bool IsReadOnly => false;

        internal BasicBlockCollection( IrFunction function )
        {
            ContainingFunction = function;
        }

        private readonly IrFunction ContainingFunction;
    }
}
