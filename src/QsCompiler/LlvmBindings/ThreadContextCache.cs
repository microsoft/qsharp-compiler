// -----------------------------------------------------------------------
// <copyright file="ThreadContextCache.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
// -----------------------------------------------------------------------

using System;

using LLVMSharp.Interop;

namespace Ubiquity.NET.Llvm
{
    /// <summary>
    /// Manages a cache of <see cref="Context"/> instances using thread local storage.
    /// </summary>
    /// <remarks>
    /// The public constructor <see cref="Context()"/> will register itself in the cache for its calling thread.
    /// Since it is a new instance, that is a safe (LLVM) operation.
    /// <para/>
    /// Q# IMPORTANT: we currently expect exactly one context per thread. This was done because we have no way
    /// to map an LLVM Metadata to its context (in Ubiquity, this is done via a custom native method).
    /// </remarks>
    internal static class ThreadContextCache
    {
        /// <summary>
        /// Retrieves the <see cref="Context"/> singleton for the current thread.
        /// </summary>
        /// <remarks>
        /// This method was added to workaround the absence of any native helper methods to get the context
        /// of an <see cref="MDNode"/> or <see cref="MDString"/> (i.e. LLVM Metadata) in the LLVM C API.
        /// </remarks>
        /// <returns>The one and only <see cref="Context"/>.</returns>
        /// <exception cref="NotSupportedException">
        /// A context is not registered for the current thread.
        /// </exception>
        internal static Context Get()
        {
            if (threadLocalInstance == null)
            {
                throw new NotSupportedException("No context is registered for the current thread.");
            }

            return threadLocalInstance;
        }

        /// <summary>
        /// Unregisters the <see cref="Context"/> associated with <paramref name="h"/> as the current
        /// thread's singleton.
        /// </summary>
        /// <param name="h">The handle of the <see cref="Context"/> to unregister.</param>
        /// <returns>
        /// True if the context associated with <paramref name="h"/> was previously registered and is now no
        /// longer. False otherwise.
        /// </returns>
        internal static bool Unregister(LLVMContextRef h)
        {
            if (threadLocalInstance?.ContextHandle == h)
            {
                threadLocalInstance = null;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Register <paramref name="context"/> as the current thread's singleton.
        /// </summary>
        /// <param name="context">The context to register.</param>
        /// <exception cref="NotSupportedException">
        /// A context is already registered with the current thread.
        /// </exception>
        internal static void Register(Context context)
        {
            if (threadLocalInstance != null && threadLocalInstance != context)
            {
                throw new NotSupportedException("A context is already associated with the current thread.");
            }

            threadLocalInstance = context;
        }

        /// <summary>
        /// Get the current thread's <see cref="Context"/> singleton, or create and register
        /// a new one using <paramref name="contextRef"/>.
        /// </summary>
        /// <remarks>
        /// If a <see cref="Context"/> is already registered for the current thread,
        /// <paramref name="contextRef"/> must match its <see cref="Context.ContextHandle"/>.
        /// </remarks>
        /// <param name="contextRef">
        /// The expected handle of the existing singleton <see cref="Context"/> if set,
        /// or the handle to use when creating the new one.
        /// </param>
        /// <returns>The <see cref="Context"/> singleton.</returns>
        /// <exception cref="NotSupportedException">
        /// The current thread has a registered <see cref="Context"/> singleton, but its
        /// <see cref="LLVMContextRef"/> does not match <paramref name="contextRef"/>.
        /// </exception>
        internal static Context GetOrCreateAndRegister(LLVMContextRef contextRef)
        {
            if (threadLocalInstance == null)
            {
                threadLocalInstance = new Context(contextRef);
                return threadLocalInstance;
            }

            if (threadLocalInstance.ContextHandle != contextRef)
            {
                throw new NotSupportedException(
                    "The context handle must match the context associated with the current thread.");
            }

            return threadLocalInstance;
        }

        [ThreadStatic]
        private static Context? threadLocalInstance;
    }
}
