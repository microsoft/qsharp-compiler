// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.Quantum.QsCompiler;
using Microsoft.VisualStudio.Threading;


namespace Microsoft.Quantum.QsLanguageServer
{
    /// Used to enforce in-order processing of the communication with the Q# language server. 
    /// Such a synchronization context is needed since the Q# language server 
    /// processes changes incrementally rather than reprocessing entire files each time.
    public class QsSynchronizationContext : SynchronizationContext
    {
        private readonly AsyncQueue<(SendOrPostCallback, object)> queued = new AsyncQueue<(SendOrPostCallback, object)>();
        private void ProcessNext()
        {
            var gotNext = this.queued.TryDequeue(out (SendOrPostCallback, object) next);
            QsCompilerError.Verify(gotNext, "nothing to process in the SynchronizationContext");
            if (gotNext) next.Item1(next.Item2);
        }

        public override void Post(SendOrPostCallback fct, object arg)
        {
            if (fct == null) throw new ArgumentNullException(nameof(fct));
            this.queued.Enqueue((fct,arg));
            base.Send(_ => ProcessNext(), null);
        }
    }
}
