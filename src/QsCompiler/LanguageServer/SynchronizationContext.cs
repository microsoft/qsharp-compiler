// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Quantum.QsCompiler;
using Microsoft.VisualStudio.Threading;

namespace Microsoft.Quantum.QsLanguageServer
{
    /// <summary>
    /// Used to enforce in-order processing of the communication with the Q# language server.
    /// Such a synchronization context is needed since the Q# language server
    /// processes changes incrementally rather than reprocessing entire files each time.
    /// </summary>
    public class QsSynchronizationContext : SynchronizationContext
    {
        private readonly AsyncQueue<(SendOrPostCallback, object?)> queued = new AsyncQueue<(SendOrPostCallback, object?)>();

        private void ProcessNext()
        {
            var gotNext = this.queued.TryDequeue(out var next);
            QsCompilerError.Verify(gotNext, "nothing to process in the SynchronizationContext");
            if (gotNext)
            {
                next.Item1(next.Item2);
            }
        }

        /// <inheritdoc/>
        public override void Post(SendOrPostCallback fct, object? arg)
        {
            this.queued.Enqueue((fct, arg));
            this.Send(_ => this.ProcessNext(), null);
        }
    }
}
