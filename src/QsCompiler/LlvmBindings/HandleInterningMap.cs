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
        private readonly IDictionary<THandle, TMappedType> HandleMap
            = new ConcurrentDictionary<THandle, TMappedType>(EqualityComparer<THandle>.Default);

        private protected HandleInterningMap(Context context)
        {
            this.Context = context;
        }

        public Context Context { get; }

        public TMappedType GetOrCreateItem(THandle handle, Action<THandle> foundHandleRelease = default)
        {
            if (this.HandleMap.TryGetValue(handle, out TMappedType retVal))
            {
                foundHandleRelease?.Invoke(handle);
                return retVal;
            }

            retVal = this.ItemFactory(handle);
            this.HandleMap.Add(handle, retVal);

            return retVal;
        }

        public void Clear()
        {
            this.DisposeItems(this.HandleMap.Values);
            this.HandleMap.Clear();
        }

        public void Remove(THandle handle)
        {
            if (this.HandleMap.TryGetValue(handle, out TMappedType item))
            {
                this.HandleMap.Remove(handle);
                this.DisposeItem(item);
            }
        }

        public IEnumerator<TMappedType> GetEnumerator() => this.HandleMap.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        // extension point to allow optimized dispose of all items if available
        // default will dispose individually
        private protected virtual void DisposeItems(ICollection<TMappedType> items)
        {
            foreach (var item in items)
            {
                this.DisposeItem(item);
            }
        }

        private protected virtual void DisposeItem(TMappedType item)
        {
            // intentional NOP for base implementation
        }

        private protected abstract TMappedType ItemFactory(THandle handle);
    }
}
