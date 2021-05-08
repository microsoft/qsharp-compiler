// -----------------------------------------------------------------------
// <copyright file="IExtensiblePropertyContainer.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Interface to allow adding arbitrary named data items to an object</summary>
    /// <remarks>
    /// It is sometimes useful for code generation applications to attach some tool specific
    /// data to the LLVM objects created but that don't need representation as LLVM Metadata
    /// nodes. This interface provides such a facility.
    /// </remarks>
    public interface IExtensiblePropertyContainer
    {
        /// <summary>Try to get a value from the container</summary>
        /// <typeparam name="T">Type of value to retrieve</typeparam>
        /// <param name="id">id of the value to retrieve</param>
        /// <param name="value">value retrieved if present (or default value of type <typeparamref name="T"/> otherwise)</param>
        /// <returns>
        /// true if the item was found and it's type matches <typeparamref name="T"/> false otherwise.
        /// </returns>
        bool TryGetExtendedPropertyValue<T>( string id, [MaybeNullWhen(false)] out T value );

        /// <summary>Adds a value to the container</summary>
        /// <param name="id">Id of the value</param>
        /// <param name="value">value to add</param>
        /// <remarks>
        /// Adds the value with the specified id. If a value with the same id
        /// already exists and its type is the same as <paramref name="value"/>
        /// it is replaced. If the existing value is of a different type, then
        /// an ArgumentException is thrown.
        /// </remarks>
        void AddExtendedPropertyValue( string id, object? value );
    }
}
