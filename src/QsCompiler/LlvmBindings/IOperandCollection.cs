// -----------------------------------------------------------------------
// <copyright file="IFixedShapeCollection.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Interface for a fixed shape collection of operands</summary>
    /// <typeparam name="T">Type of elements in the container</typeparam>
    /// <remarks>
    /// This interface describes a subset of the behavior of <see cref="ICollection{T}"/>
    /// and <see cref="IList{T}"/> along with an extension of the behavior of <see cref="IReadOnlyList{T}"/>.
    /// The semantics are a collection where the size/shape is not mutable, however the
    /// individual members are. That is the container does not support adding or removing
    /// elements, but does allow replacing existing elements.
    /// </remarks>
    public interface IOperandCollection<T>
        : IReadOnlyCollection<T>
    {
        /// <summary>Gets or sets the specified element in the collection</summary>
        /// <param name="index">index of the element in the collection</param>
        /// <returns>The element in the collection</returns>
        T this[int index] { get; set; }
    }
}
