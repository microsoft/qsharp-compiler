// -----------------------------------------------------------------------
// <copyright file="ValueAttributeCollection.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Ubiquity.NET.Llvm.Values
{
    internal class ValueAttributeCollection
        : ICollection<AttributeValue>
    {
        private readonly IAttributeAccessor container;
        private readonly FunctionAttributeIndex index;

        public ValueAttributeCollection(IAttributeAccessor container, FunctionAttributeIndex index)
        {
            this.container = container;
            this.index = index;
        }

        public int Count => (int)this.container.GetAttributeCountAtIndex(this.index);

        public bool IsReadOnly => false;

        public void Add(AttributeValue item)
        {
            this.container.AddAttributeAtIndex(this.index, item);
        }

        public void Clear()
        {
            foreach (AttributeValue attrib in this)
            {
                this.Remove(attrib);
            }
        }

        public bool Contains(AttributeValue item)
        {
            return this.Any(a => a == item);
        }

        public void CopyTo(AttributeValue[] array, int arrayIndex)
        {
            /* ReSharper disable ConditionIsAlwaysTrueOrFalse */
            /* ReSharper disable HeuristicUnreachableCode */
            if (array == default)
            {
                return;
            }

            /* ReSharper enable HeuristicUnreachableCode */
            /* ReSharper enable ConditionIsAlwaysTrueOrFalse */

            foreach (AttributeValue attribute in this)
            {
                array[arrayIndex] = attribute;
                ++arrayIndex;
            }
        }

        public IEnumerator<AttributeValue> GetEnumerator()
        {
            return this.container.GetAttributesAtIndex(this.index).GetEnumerator();
        }

        public bool Remove(AttributeValue item)
        {
            bool retVal = this.Contains(item);
            if (item.IsEnum)
            {
                this.container.RemoveAttributeAtIndex(this.index, item.Kind);
            }
            else
            {
                this.container.RemoveAttributeAtIndex(this.index, item.Name);
            }

            return retVal;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
