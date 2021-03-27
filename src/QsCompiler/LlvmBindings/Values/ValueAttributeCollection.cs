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
        public ValueAttributeCollection(IAttributeAccessor container, FunctionAttributeIndex index)
        {
            this.Container = container;
            this.Index = index;
        }

        public int Count => (int)this.Container.GetAttributeCountAtIndex(this.Index);

        public bool IsReadOnly => false;

        public void Add(AttributeValue item)
        {
            this.Container.AddAttributeAtIndex(this.Index, item);
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
            return this.Container.GetAttributesAtIndex(this.Index).GetEnumerator();
        }

        public bool Remove(AttributeValue item)
        {
            bool retVal = this.Contains(item);
            if (item.IsEnum)
            {
                this.Container.RemoveAttributeAtIndex(this.Index, item.Kind);
            }
            else
            {
                this.Container.RemoveAttributeAtIndex(this.Index, item.Name);
            }

            return retVal;
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private readonly IAttributeAccessor Container;
        private readonly FunctionAttributeIndex Index;
    }
}
