// <copyright file="Library.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using LlvmBindings.Interop;

namespace LlvmBindings.Interop
{
    /// <summary>Provides support for various LLVM static state initialization and manipulation.</summary>
    public sealed class Library
        : DisposableObject,
        ILibLlvm
    {
        private static int currentInitializationState;

        // lazy initialized singleton unmanaged delegate so it is never collected
        private static Lazy<LLVMFatalErrorHandler>? fatalErrorHandlerDelegate;

        private Library()
        {
        }

        private enum InitializationState
        {
            Uninitialized,
            Initializing,
            Initialized,
            ShuttingDown,
            ShutDown, // NOTE: This is a terminal state, it doesn't return to uninitialized
        }

        /// <summary>Initializes the native LLVM library support.</summary>
        /// <returns>
        /// <see cref="IDisposable"/> implementation for the library.
        /// </returns>
        /// <remarks>
        /// This can only be called once per application to initialize the
        /// LLVM library. <see cref="IDisposable.Dispose()"/> will release
        /// any resources allocated by the library. The current LLVM library does
        /// *NOT* support re-initialization within the same process. Thus, this
        /// is best used at the top level of the application and released at or
        /// near process exit.
        /// </remarks>
        public static ILibLlvm InitializeLLVM()
        {
            var previousState = (InitializationState)Interlocked.CompareExchange(
                ref currentInitializationState,
                (int)InitializationState.Initializing,
                (int)InitializationState.Uninitialized);
            if (previousState != InitializationState.Uninitialized)
            {
                throw new InvalidOperationException();
            }

            // initialize the static fields
            unsafe
            {
                fatalErrorHandlerDelegate = new Lazy<LLVMFatalErrorHandler>(() => FatalErrorHandler, LazyThreadSafetyMode.PublicationOnly);
            }

            LLVM.InstallFatalErrorHandler(Marshal.GetFunctionPointerForDelegate(fatalErrorHandlerDelegate.Value));
            Interlocked.Exchange(ref currentInitializationState, (int)InitializationState.Initialized);
            return new Library();
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            InternalShutdownLLVM();
        }

        private static unsafe void FatalErrorHandler(sbyte* reason)
        {
            // NOTE: LLVM will call exit() upon return from this function and there's no way to stop it
            Trace.TraceError("LLVM Fatal Error: '{0}'; Application will exit.", new string((char*)reason));
        }

        private static void InternalShutdownLLVM()
        {
            var previousState = (InitializationState)Interlocked.CompareExchange(
                ref currentInitializationState,
                (int)InitializationState.ShuttingDown,
                (int)InitializationState.Initialized);
            if (previousState != InitializationState.Initialized)
            {
                throw new InvalidOperationException();
            }

            LLVM.Shutdown();

            Interlocked.Exchange(ref currentInitializationState, (int)InitializationState.ShutDown);
        }
    }
}
