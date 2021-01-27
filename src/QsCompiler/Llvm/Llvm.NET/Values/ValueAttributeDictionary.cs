// -----------------------------------------------------------------------
// <copyright file="ValueAttributeDictionary.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>re-usable implementation of IAttributeDictionary for containers that implement IAttributeAccessor</summary>
    /// <remarks>
    /// This uses the low-level methods of IAttributeAccessor to abstract out the differences in the
    /// LLVM-C API for attributes on CallSites vs. Functions
    /// </remarks>
    internal class ValueAttributeDictionary
        : IAttributeDictionary
    {
        public Context Context => Container.Context;

        public ICollection<AttributeValue> this[ FunctionAttributeIndex key ]
        {
            get
            {
                if( !ContainsKey( key ) )
                {
                    throw new KeyNotFoundException( );
                }

                return new ValueAttributeCollection( Container, key );
            }
        }

        public IEnumerable<FunctionAttributeIndex> Keys
            => new ReadOnlyCollection<FunctionAttributeIndex>( GetValidKeys( ).ToList( ) );

        public IEnumerable<ICollection<AttributeValue>> Values
            => new ReadOnlyCollection<ICollection<AttributeValue>>( this.Select( kvp => kvp.Value ).ToList( ) );

        public int Count => GetValidKeys( ).Count( );

        public bool ContainsKey( FunctionAttributeIndex key ) => GetValidKeys( ).Any( k => k == key );

        public IEnumerator<KeyValuePair<FunctionAttributeIndex, ICollection<AttributeValue>>> GetEnumerator( )
        {
            return ( from key in GetValidKeys( )
                     select new KeyValuePair<FunctionAttributeIndex, ICollection<AttributeValue>>( key, this[ key ] )
                   ).GetEnumerator( );
        }

        public bool TryGetValue( FunctionAttributeIndex key, /*[MaybeNullWhen( false )]*/ out ICollection<AttributeValue> value )
        {
            // sadly the runtime provided interface doesn't correctly apply the MaybeNullWhen attribute,
            // and the compiler generates warning:
            // CS8767: Nullability of reference types in type of parameter 'value' of 'bool ValueAttributeDictionary.TryGetValue(FunctionAttributeIndex key, out ICollection<AttributeValue> value)' doesn't match implicitly implemented member 'bool IReadOnlyDictionary<FunctionAttributeIndex, ICollection<AttributeValue>>.TryGetValue(FunctionAttributeIndex key, out ICollection<AttributeValue> value)' because of nullability attributes.
            // Yeah, clear as mud, right?
            // So, use the ! to silence the compiler and don't use the attribute, sigh... what a mess...
            value = null!;
            if( ContainsKey( key ) )
            {
                return false;
            }

            value = new ValueAttributeCollection( Container, key );
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        internal ValueAttributeDictionary( IAttributeAccessor container, Func<IrFunction> functionFetcher )
        {
            Container = container;
            FunctionFetcher = functionFetcher;
        }

        private IEnumerable<FunctionAttributeIndex> GetValidKeys( )
        {
            var endIndex = FunctionAttributeIndex.Parameter0 + FunctionFetcher().Parameters.Count;
            for( var index = FunctionAttributeIndex.Function; index < endIndex; ++index )
            {
                if( Container.GetAttributeCountAtIndex( index ) > 0 )
                {
                    yield return index;
                }
            }
        }

        private readonly Func<IrFunction> FunctionFetcher;
        private readonly IAttributeAccessor Container;
    }
}
