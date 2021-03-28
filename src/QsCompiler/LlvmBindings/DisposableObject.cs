// <copyright file="DisposableObject.cs" company="Ubiquity.NET Contributors">
// Copyright (c) Ubiquity.NET Contributors. All rights reserved.
// Portions Copyright (c) Microsoft Corporation
// </copyright>
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Ubiquity.NET.Llvm.Interop
{
    /// <summary>Abstract base class for implementing the Disposable pattern.</summary>
    public abstract class DisposableObject
        : IDisposable
    {
        // do not write directly to this field, it should only be done with interlocked calls in Dispose() to ensure correct behavior
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Indicates the field should never be directly written to")]
        private int isDisposed_;

        /// <summary>Finalizes an instance of the <see cref="DisposableObject"/> class. This releases any unmanaged resources it owns</summary>
        ~DisposableObject()
        {
            this.Dispose(false);
        }

        /// <summary>Gets a value indicating whether the object is disposed or not.</summary>
        public bool IsDisposed => this.isDisposed_ != 0;

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "This guarantees dispose is idempotent")]
        public void Dispose()
        {
            bool needsDispose = Interlocked.Exchange(ref this.isDisposed_, 1) == 0;
            if (needsDispose)
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>Abstract method that is implemented by derived types to perform the dispose operation.</summary>
        /// <param name="disposing">Indicates if this is a dispose or finalize operation.</param>
        /// <remarks>
        /// This is guaranteed to only be called if <see cref="IsDisposed"/> returns <see langword="false"/>
        /// so the implementation should only be concerned with the actual release of resources. If <paramref name="disposing"/>
        /// is <see langword="true"/> then the implementation should release managed and unmanaged resources, otherwise it should
        /// only release the unmanaged resources.
        /// </remarks>
        protected abstract void Dispose(bool disposing);
    }
}
