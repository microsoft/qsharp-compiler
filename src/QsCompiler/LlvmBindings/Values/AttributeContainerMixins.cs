// -----------------------------------------------------------------------
// <copyright file="AttributeContainerMixins.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Ubiquity.NET.Llvm.Values
{
    /// <summary>Provides a layer of simplicity and backwards compatibility for manipulating attributes on Values.</summary>
    /// <remarks>At some point when Default interface methods are available (proposed for C#8) these methods can leverage that.</remarks>
    public static class AttributeContainerMixins
    {
        /// <summary>Determines if a collection of <see cref="AttributeValue"/> contains a given <see cref="AttributeKind"/>.</summary>
        /// <param name="self">Collection to test.</param>
        /// <param name="kind"><see cref="AttributeKind"/> to search for.</param>
        /// <returns><see langword="true"/> if found.</returns>
        public static bool Contains(this ICollection<AttributeValue> self, AttributeKind kind)
        {
            return self.Any(a => a.Kind == kind);
        }

        /// <summary>Adds attributes to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attributes to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attributes to.</param>
        /// <param name="values">Attributes to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttributes<T>(this T self, FunctionAttributeIndex index, params AttributeKind[] values)
            where T : class, IAttributeContainer
        {
            if (values != default)
            {
                foreach (var kind in values)
                {
                    AttributeValue attrib = self.Context.CreateAttribute(kind);
                    if (self is IAttributeAccessor container)
                    {
                        container.AddAttributeAtIndex(index, attrib);
                    }
                    else
                    {
                        self.Attributes[index].Add(attrib);
                    }
                }
            }

            return self;
        }

        /// <summary>Adds a single <see cref="AttributeKind"/> to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attribute to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attribute to.</param>
        /// <param name="kind">Attribute to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttribute<T>(this T self, FunctionAttributeIndex index, AttributeKind kind)
            where T : class, IAttributeContainer
        {
            AttributeValue attrib = self.Context.CreateAttribute(kind);
            if (self is IAttributeAccessor container)
            {
                container.AddAttributeAtIndex(index, attrib);
            }
            else
            {
                self.Attributes[index].Add(self.Context.CreateAttribute(kind));
            }

            return self;
        }

        /// <summary>Adds a single <see cref="AttributeValue"/> to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attribute to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attribute to.</param>
        /// <param name="attrib">Attribute to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttribute<T>(this T self, FunctionAttributeIndex index, AttributeValue attrib)
            where T : class, IAttributeContainer
        {
            if (self is IAttributeAccessor container)
            {
                container.AddAttributeAtIndex(index, attrib);
            }
            else
            {
                self.Attributes[index].Add(attrib);
            }

            return self;
        }

        /// <summary>Adds <see cref="AttributeValue"/>s to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attribute to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attributes to.</param>
        /// <param name="attributes">Attribute to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttributes<T>(this T self, FunctionAttributeIndex index, params AttributeValue[] attributes)
            where T : class, IAttributeContainer
        {
            return AddAttributes(self, index, (IEnumerable<AttributeValue>)attributes);
        }

        /// <summary>Adds <see cref="AttributeValue"/>s to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attribute to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attributes to.</param>
        /// <param name="attributes">Attribute to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttributes<T>(this T self, FunctionAttributeIndex index, IEnumerable<AttributeValue> attributes)
            where T : class, IAttributeContainer
        {
            if (attributes != default)
            {
                foreach (var attrib in attributes)
                {
                    if (self is IAttributeAccessor container)
                    {
                        container.AddAttributeAtIndex(index, attrib);
                    }
                    else
                    {
                        self.Attributes[index].Add(attrib);
                    }
                }
            }

            return self;
        }

        /// <summary>Adds the attributes from and <see cref="IAttributeDictionary"/> to an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to add the attributes to.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to add the attributes to.  </param>
        /// <param name="attributes"><see cref="IAttributeDictionary"/> containing the attributes to add to the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T AddAttributes<T>(this T self, FunctionAttributeIndex index, IAttributeDictionary attributes)
            where T : class, IAttributeContainer
        {
            return AddAttributes(self, index, attributes[index]);
        }

        /// <summary>Removes an <see cref="AttributeKind"/> from an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to remove the attribute from.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to remove the attribute from. </param>
        /// <param name="kind">Attribute to remove from the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T RemoveAttribute<T>(this T self, FunctionAttributeIndex index, AttributeKind kind)
            where T : class, IAttributeContainer
        {
            if (kind == AttributeKind.None)
            {
                return self;
            }

            if (self is IAttributeAccessor container)
            {
                container.RemoveAttributeAtIndex(index, kind);
            }
            else
            {
                ICollection<AttributeValue> attributes = self.Attributes[index];
                AttributeValue attrib = attributes.FirstOrDefault(a => a.Kind == kind);
                if (attrib != default)
                {
                    attributes.Remove(attrib);
                }
            }

            return self;
        }

        /// <summary>Removes a named attribute from an <see cref="IAttributeContainer"/>.</summary>
        /// <typeparam name="T">Container type.</typeparam>
        /// <param name="self">Container to remove the attribute from.</param>
        /// <param name="index"><see cref="FunctionAttributeIndex"/> to remove the attribute from. </param>
        /// <param name="name">Attribute name to remove from the container.</param>
        /// <returns><paramref name="self"/> for fluent use.</returns>
        public static T RemoveAttribute<T>(this T self, FunctionAttributeIndex index, string name)
            where T : class, IAttributeContainer
        {
            if (self is IAttributeAccessor container)
            {
                container.RemoveAttributeAtIndex(index, name);
            }
            else
            {
                ICollection<AttributeValue> attributes = self.Attributes[index];
                AttributeValue attrib = attributes.FirstOrDefault(a => a.Name == name);
                if (attrib != default)
                {
                    attributes.Remove(attrib);
                }
            }

            return self;
        }
    }
}
