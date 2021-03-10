// -----------------------------------------------------------------------
// <copyright file="HandleInterningMap.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ubiquity.NET.Llvm
{
    internal abstract class HandleInterningMap<THandle, TMappedType>
        : IHandleInterning<THandle, TMappedType>
    {
        public Context Context { get; }

        public TMappedType GetOrCreateItem( THandle handle, Action<THandle> foundHandleRelease = default )
        {
            if( HandleMap.TryGetValue( handle, out TMappedType retVal ) )
            {
                foundHandleRelease?.Invoke( handle );
                return retVal;
            }

            retVal = ItemFactory( handle );
            HandleMap.Add( handle, retVal );

            return retVal;
        }

        public void Clear( )
        {
            DisposeItems( HandleMap.Values );
            HandleMap.Clear( );
        }

        public void Remove( THandle handle )
        {
            if( HandleMap.TryGetValue( handle, out TMappedType item ) )
            {
                HandleMap.Remove( handle );
                DisposeItem( item );
            }
        }

        public IEnumerator<TMappedType> GetEnumerator( ) => HandleMap.Values.GetEnumerator( );

        IEnumerator IEnumerable.GetEnumerator( ) => GetEnumerator( );

        private protected HandleInterningMap( Context context )
        {
            Context = context;
        }

        // extension point to allow optimized dispose of all items if available
        // default will dispose individually
        private protected virtual void DisposeItems( ICollection<TMappedType> items )
        {
            foreach( var item in items )
            {
                DisposeItem( item );
            }
        }

        private protected virtual void DisposeItem( TMappedType item )
        {
            // intentional NOP for base implementation
        }

        private protected abstract TMappedType ItemFactory( THandle handle );

        private readonly IDictionary<THandle, TMappedType> HandleMap
            = new ConcurrentDictionary<THandle, TMappedType>( EqualityComparer<THandle>.Default );
    }
}
