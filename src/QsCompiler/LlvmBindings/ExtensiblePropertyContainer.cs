// -----------------------------------------------------------------------
// <copyright file="ExtensiblePropertyContainer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Common implementation of <see cref="IExtensiblePropertyContainer"/></summary>
    /// <remarks>
    /// This class implements <see cref="IExtensiblePropertyContainer"/> through an
    /// internal <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>
    /// </remarks>
    public class ExtensiblePropertyContainer
        : IExtensiblePropertyContainer
    {
        /// <inheritdoc/>
        public void AddExtendedPropertyValue( string id, object? value )
        {
            lock( Items )
            {
                if( Items.TryGetValue( id, out object? currentValue ) )
                {
                    if( currentValue != null && value != null && currentValue.GetType( ) != value.GetType( ) )
                    {
                        throw new ArgumentException();
                    }
                }

                Items[ id ] = value;
            }
        }

        /// <inheritdoc/>
        public bool TryGetExtendedPropertyValue<T>( string id, [MaybeNullWhen( false )] out T value )
        {
            value = default!;
            object? item;
            lock( Items )
            {
                if( !Items.TryGetValue( id, out item ) )
                {
                    return false;
                }
            }

            if( !( item is T ) )
            {
                return false;
            }

            value = ( T )item;
            return true;
        }

        private readonly Dictionary<string, object?> Items = new Dictionary<string, object?>();
    }
}
