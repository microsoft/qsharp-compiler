// -----------------------------------------------------------------------
// <copyright file="ContextCache.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Maintains a global cache of <see cref="LLVMContextRef"/> to <see cref="Context"/> mappings</summary>
    /// <remarks>
    /// The public constructor <see cref="Context()"/> will add itself to the cache, since it is a new instance
    /// that is a safe operation. In all other cases a lookup in the cache based on the underlying LLVM handle is
    /// performed in a thread safe manner.
    /// </remarks>
    internal static class ContextCache
    {
        internal static bool TryRemove( LLVMContextRef h )
        {
            return Instance.Value.TryRemove( h, out Context _ );
        }

        internal static void Add( Context context )
        {
            if( !Instance.Value.TryAdd( context.ContextHandle, context ) )
            {
                throw new InternalCodeGeneratorException( "Internal Error: Can't add context to Cache as it already exists!" );
            }
        }

        internal static Context GetContextFor( LLVMContextRef contextRef )
        {
            return Instance.Value.GetOrAdd( contextRef, h => new Context( h ) );
        }

        private static ConcurrentDictionary<LLVMContextRef, Context> CreateInstance( )
        {
            return new ConcurrentDictionary<LLVMContextRef, Context>( EqualityComparer<LLVMContextRef>.Default );
        }

        private static readonly Lazy<ConcurrentDictionary<LLVMContextRef, Context>> Instance
            = new Lazy<ConcurrentDictionary<LLVMContextRef, Context>>( CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
