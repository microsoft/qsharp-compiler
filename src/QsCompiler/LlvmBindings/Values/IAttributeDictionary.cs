// -----------------------------------------------------------------------
// <copyright file="AttributeCollectionExtensions.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Interface to an Attribute Dictionary.</summary>
    /// <remarks>
    /// <para>This interface provides a full collection of all the
    /// attributes keyed by the <see cref="FunctionAttributeIndex"/>.
    /// </para>
    /// <note>This conceptually corresponds to the functionality of the
    /// LLVM AttributeSet class for Versions prior to 5. In LLVM 5 the
    /// equivalent type is currently AttributeList. In v5 AttributeSet
    /// has no index and is therefore more properly a set than in the
    /// past. To help remove confusion and satisfy .NET naming rules this
    /// is called a Dictionary as that reflects the use here and fits
    /// the direction of LLVM.</note>
    /// </remarks>
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "It has the correct suffix, it's a dictionary, not just a collection")]
    public interface IAttributeDictionary
        : IReadOnlyDictionary<FunctionAttributeIndex, ICollection<AttributeValue>>
    {
    }

    /// <summary>Interface for objects that contain Attributes.</summary>
    public interface IAttributeContainer
    {
        /// <summary>Gets the <see cref="Llvm.Context"/> that owns these attributes. </summary>
        Context Context { get; }

        /// <summary>Gets the full set of Attributes keyed by <see cref="FunctionAttributeIndex"/>.</summary>
        IAttributeDictionary Attributes { get; }
    }

    /// <summary>Extension methods for a collection of <see cref="AttributeValue"/>.</summary>
    public static class AttributeCollectionExtensions
    {
        /// <summary>Removes an attribute from a collection.</summary>
        /// <param name="set">Attribute collection (set) to remove the attribute from.</param>
        /// <param name="kind"><see cref="AttributeKind"/> to remove.</param>
        /// <returns><see langword="true"/> if <paramref name="kind"/> was in <paramref name="set"/> before being removed.</returns>
        public static bool Remove(this ICollection<AttributeValue> set, AttributeKind kind)
        {
            return Remove(set, kind.GetAttributeName());
        }

        /// <summary>Removes an attribute from a collection.</summary>
        /// <param name="set">Attribute collection (set) to remove the attribute from.</param>
        /// <param name="name">Name of the attribute to remove.</param>
        /// <returns><see langword="true"/> if <paramref name="name"/> was in <paramref name="set"/> before being removed.</returns>
        public static bool Remove(this ICollection<AttributeValue> set, string name)
        {
            if (set == default)
            {
                throw new ArgumentNullException(nameof(set));
            }

            var attr = (from a in set
                        where a.Name == name
                        select a)
                       .FirstOrDefault();

            if (attr == default)
            {
                return false;
            }

            set.Remove(attr);
            return true;
        }
    }
}
