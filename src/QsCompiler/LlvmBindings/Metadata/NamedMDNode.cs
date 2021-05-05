// -----------------------------------------------------------------------
// <copyright file="NamedMDNode.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Ubiquity.ArgValidators;
using Ubiquity.NET.Llvm.Interop;

using static Ubiquity.NET.Llvm.Interop.NativeMethods;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Wraps an LLVM NamedMDNode</summary>
    /// <remarks>Despite its name a NamedMDNode is not itself an MDNode. It is owned directly by a
    /// a <see cref="BitcodeModule"/> and contains a list of <see cref="MDNode"/> operands.</remarks>
    public class NamedMDNode
    {
        /// <summary>Gets the name of the node</summary>
        public string Name => LLVMGetNamedMetadataName( NativeHandle, out size_t _ );

        /// <summary>Gets the operands for the node</summary>
        public IList<MDNode> Operands { get; }

        /// <summary>Gets the module that owns this node</summary>
        public BitcodeModule ParentModule => BitcodeModule.FromHandle( LibLLVMNamedMetadataGetParentModule( NativeHandle ).ThrowIfInvalid( ) )!;

        /// <summary>Erases this node from its parent</summary>
        public void EraseFromParent( ) => LibLLVMNamedMetadataEraseFromParent( NativeHandle );

        internal NamedMDNode( LLVMNamedMDNodeRef nativeNode )
        {
            NativeHandle = nativeNode;
            Operands = new OperandIterator( this );
        }

        // private iterator for Metadata operands
        private class OperandIterator
            : IList<MDNode>
        {
            public MDNode this[ int index ]
            {
                get
                {
                    index.ValidateRange( 0, Count, nameof( index ) );
                    var nodeHanlde = LibLLVMNamedMDNodeGetOperand( OwningNode.NativeHandle, ( uint )index );
                    return LlvmMetadata.FromHandle<MDNode>( OwningNode.ParentModule.Context, nodeHanlde.ThrowIfInvalid( ) )!;
                }

                set
                {
                    index.ValidateRange( 0, Count, nameof( index ) );
                    LibLLVMNamedMDNodeSetOperand( OwningNode.NativeHandle, ( uint )index, value.MetadataHandle );
                }
            }

            public int Count => ( int )LibLLVMNamedMDNodeGetNumOperands( OwningNode.NativeHandle );

            public IEnumerator<MDNode> GetEnumerator( )
            {
                for( int i = 0; i < Count; ++i )
                {
                    yield return this[ i ];
                }
            }

            IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

            public int IndexOf( MDNode item )
            {
                for( int i = 0; i < Count; ++i )
                {
                    if( this[ i ] == item )
                    {
                        return i;
                    }
                }

                return -1;
            }

            public void Add( MDNode item )
            {
                item.ValidateNotNull( nameof( item ) );
                LibLLVMNamedMDNodeAddOperand( OwningNode.NativeHandle, item.MetadataHandle );
            }

            public void Clear( )
            {
                LibLLVMNamedMDNodeClearOperands( OwningNode.NativeHandle );
            }

            public bool Contains( MDNode item ) => this.Any( n => n == item );

            public void CopyTo( MDNode[ ] array, int arrayIndex )
            {
                arrayIndex.ValidateRange( 0, array.Length - Count, nameof( arrayIndex ) );
                for( int i = 0; i < Count; ++i )
                {
                    array[ i + arrayIndex ] = this[ i ];
                }
            }

            public bool Remove( MDNode item )
            {
                throw new NotSupportedException( );
            }

            public void Insert( int index, MDNode item )
            {
                throw new NotSupportedException( );
            }

            public void RemoveAt( int index )
            {
                throw new NotSupportedException( );
            }

            public bool IsReadOnly => false;

            internal OperandIterator( NamedMDNode owner )
            {
                OwningNode = owner;
            }

            private readonly NamedMDNode OwningNode;
        }

        private readonly LLVMNamedMDNodeRef NativeHandle;
    }
}
