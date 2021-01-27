// -----------------------------------------------------------------------
// <copyright file="IHandleInterning.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

// Internal types don't require XML docs, despite settings in stylecop.json the analyzer still
// gripes about these for interfaces...
#pragma warning disable SA1600

namespace Ubiquity.NET.Llvm
{
    internal interface IHandleInterning<THandle, TMappedType>
        : IEnumerable<TMappedType>
    {
        Context Context { get; }

        TMappedType GetOrCreateItem( THandle handle, Action<THandle>? foundHandleRelease = null );

        void Remove( THandle handle );

        void Clear( );
    }
}
