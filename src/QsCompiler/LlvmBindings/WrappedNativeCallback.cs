// <copyright file="WrappedNativeCallback.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Ubiquity.NET.Llvm.Interop
{
    /// <summary>Keep alive holder to ensure native call back delegates are not destroyed while registered with native code.</summary>
    /// <remarks>
    /// This generates a holder for a delegate that allows a native function pointer for the delegate to remain valid until the
    /// instance of this wrapper is disposed. This is generally only necessary where the native call back must remain valid for
    /// an extended period of time. (e.g. beyond the call that provides the callback).
    ///
    /// <note type="note">
    /// This doesn't actually pin the delegate, but it does add
    /// an additional reference
    /// see: https://docs.microsoft.com/en-us/cpp/dotnet/how-to-marshal-callbacks-and-delegates-by-using-cpp-interop for more info.
    /// </note>
    /// </remarks>
    public abstract class WrappedNativeCallback
        : DisposableObject
    {
        private readonly IntPtr nativeFuncPtr;

        private readonly GCHandle handle;

        // keeps a live ref for the delegate around so GC won't clean it up
        private Delegate? unpinnedDelegate;

        /// <summary>Initializes a new instance of the <see cref="WrappedNativeCallback"/> class.</summary>
        /// <param name="d">Delegate.</param>
        protected internal WrappedNativeCallback(Delegate d)
        {
            if (d.GetType().IsGenericType)
            {
                // Marshal.GetFunctionPointerForDelegate will create an exception for this but the
                // error message is, pardon the pun, a bit too generic. Hopefully, this makes it a
                // bit more clear.
                throw new ArgumentException();
            }

            if (d.GetType().GetCustomAttributes(typeof(UnmanagedFunctionPointerAttribute), true).Length == 0)
            {
                throw new ArgumentException();
            }

            this.unpinnedDelegate = d;
            this.handle = GCHandle.Alloc(this.unpinnedDelegate);
            this.nativeFuncPtr = Marshal.GetFunctionPointerForDelegate(this.unpinnedDelegate);
        }

        /// <summary>Converts a callback to an IntPtr suitable for passing to native code.</summary>
        /// <param name="cb">Callback to cast to an <see cref="IntPtr"/>.</param>
        public static implicit operator IntPtr(WrappedNativeCallback cb) => cb.ToIntPtr();

        /// <summary>Gets the raw native function pointer for the pinned delegate.</summary>
        /// <returns>Native callable function pointer.</returns>
        public IntPtr ToIntPtr() => this.nativeFuncPtr;

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            this.handle.Free();
            this.unpinnedDelegate = default;
        }

        /// <summary>Gets a delegate from the raw native callback.</summary>
        /// <typeparam name="T">Type of delegate to convert to.</typeparam>
        /// <returns>Delegate suitable for passing as an "in" parameter to native methods.</returns>
        protected T ToDelegate<T>()
            where T : Delegate
        {
            return (T)Marshal.GetDelegateForFunctionPointer(this.ToIntPtr(), typeof(T));
        }
    }

    /// <summary>Keep alive holder to ensure native call back delegates are not destroyed while registered with native code.</summary>
    /// <typeparam name="T">Delegate signature of the native callback.</typeparam>
    /// <remarks>
    /// This generates a holder for a delegate that allows a native function pointer for the delegate to remain valid until the
    /// instance of this wrapper is disposed. This is generally only necessary where the native call back must remain valid for
    /// an extended period of time. (e.g. beyond the call that provides the callback).
    ///
    /// <note type="note">
    /// This doesn't actually pin the delegate, but it does add
    /// an additional reference
    /// see: https://msdn.microsoft.com/en-us/library/367eeye0.aspx for more info.
    /// </note>
    /// </remarks>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Simple generic variant")]
    public sealed class WrappedNativeCallback<T>
        : WrappedNativeCallback
        where T : Delegate
    {
        /// <summary>Initializes a new instance of the <see cref="WrappedNativeCallback{T}"/> class.</summary>
        /// <param name="d">Delegate to keep alive until this instance is disposed.</param>
        public WrappedNativeCallback(T d)
            : base(d)
        {
        }

        /// <summary>Gets a delegate from the raw native callback.</summary>
        /// <param name="cb">Callback to get the delegate for.</param>
        /// <returns>Delegate suitable for passing as an "in" parameter to native methods.</returns>
        [SuppressMessage("Usage", "CA2225:Operator overloads have named alternates", Justification = "ToDelegate serves the purpose without confusion on generic parameter name")]
        public static implicit operator T(WrappedNativeCallback<T> cb) => cb.ToDelegate();

        /// <summary>Gets a delegate from the raw native callback.</summary>
        /// <returns>Delegate suitable for passing as an "in" parameter to native methods.</returns>
        public T ToDelegate() => this.ToDelegate<T>();
    }
}
