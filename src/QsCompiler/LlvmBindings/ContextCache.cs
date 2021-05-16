// -----------------------------------------------------------------------
// <copyright file="ContextCache.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------
#define SINGLE_CONTEXT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>Maintains a global cache of <see cref="LLVMContextRef"/> to <see cref="Context"/> mappings.</summary>
    /// <remarks>
    /// The public constructor <see cref="Context()"/> will add itself to the cache, since it is a new instance
    /// that is a safe operation. In all other cases a lookup in the cache based on the underlying LLVM handle is
    /// performed in a thread safe manner.
    /// </remarks>
    internal static class ContextCache
    {
        /// <summary>
        /// Ensures that <paramref name="context"/> is registed as the one and only <see cref="Context"/>
        /// in the cache.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Another <see cref="Context"/> with a different LLVM handle has already been registered.
        /// </exception>
        /// <param name="context">The <see cref="Context"/> to register.</param>
        [Conditional("SINGLE_CONTEXT")]
        private static void TryInitializeSingleton(Context context)
        {
            var single = Instance.Value.GetOrAdd(default, h => context);
            if (single.ContextHandle != context.ContextHandle)
            {
                throw new NotSupportedException($"Context already exists!");
            }
        }

#if SINGLE_CONTEXT
        /// <summary>
        /// Retrieves the <see cref="Context"/> singleton.
        /// </summary>
        /// <remarks>
        /// (Q#) This method was added to workaround the absence of any native helper methods to get the context
        /// of an <see cref="MDNode"/> or <see cref="MDString"/> (i.e. LLVM Metadata) in the LLVM C API.
        /// </remarks>
        /// <returns>The one and only <see cref="Context"/>.</returns>
        internal static Context Single()
        {
            if (Instance.Value.TryGetValue(default, out var result))
            {
                return result;
            }

            throw new NotSupportedException("Context does not exist!");
        }
#endif

        internal static bool TryRemove(LLVMContextRef h)
        {
            return Instance.Value.TryRemove(h, out Context _);
        }

        internal static void Add(Context context)
        {
            TryInitializeSingleton(context);
            if (!Instance.Value.TryAdd(context.ContextHandle, context))
            {
                throw new InternalCodeGeneratorException("Internal Error: Can't add context to Cache as it already exists!");
            }
        }

        internal static Context GetContextFor(LLVMContextRef contextRef)
        {
            var context = new Context(contextRef);
            TryInitializeSingleton(context);
            return Instance.Value.GetOrAdd(contextRef, h => context);
        }

        private static ConcurrentDictionary<LLVMContextRef, Context> CreateInstance()
        {
            return new ConcurrentDictionary<LLVMContextRef, Context>(EqualityComparer<LLVMContextRef>.Default);
        }

        private static readonly Lazy<ConcurrentDictionary<LLVMContextRef, Context>> Instance
            = new Lazy<ConcurrentDictionary<LLVMContextRef, Context>>(CreateInstance, LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
