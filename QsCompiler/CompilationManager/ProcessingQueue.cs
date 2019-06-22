// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;


namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// Class allowing to enforce in-order processing of tasks. 
    public class ProcessingQueue
    {
        /// header string for the message given when an exception if logged
        private readonly string ExceptionHeader;
        /// used to log exceptions raised during processing
        private readonly Action<Exception> LogException;
        /// schedulers both for tasks that have to run sequentially and for tasks that can run simultaneously
        private readonly ConcurrentExclusiveSchedulerPair Scheduler;

        public ProcessingQueue(Action<Exception> exceptionLogger, string exceptionHeader = null)
        {
            this.ExceptionHeader = exceptionHeader ?? "error while running queued task";
            this.LogException = exceptionLogger ?? Console.Error.WriteLine;
            this.Scheduler = new ConcurrentExclusiveSchedulerPair();
        }

        private Task ProcessingTaskAsync(Action action) => 
            new Task(() =>
            {
                try { QsCompilerError.RaiseOnFailure(action, this.ExceptionHeader); }
                catch (Exception ex) { this.LogException(ex); }
            });

        /// The queue will not accept any more tasks after calling complete. 
        public void Complete() =>
            this.Scheduler.Complete();


        // non-concurrent routines - i.e. routines that are forced to execute in order

        /// Enqueues the given Action for exclusive (serialized) execution. 
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Throws an ArgumentNullException if the given Action is null.
        public Task QueueForExecutionAsync(Action processing)
        {
            if (processing == null) throw new ArgumentNullException(nameof(processing));
            var processingTask = ProcessingTaskAsync(processing);
            processingTask.Start(this.Scheduler.ExclusiveScheduler);
            return processingTask;
        }

        /// Executes the given Action synchronously, with no exclusive actions running.
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Throws an ArgumentNullException if the given Action is null.
        /// NOTE: may deadlock if the given function to execute calls this processing queue. 
        public void QueueForExecution(Action processing)
        {
            if (processing == null) throw new ArgumentNullException(nameof(processing));
            this.QueueForExecution(() => { processing(); return new object(); }, out var _);
        }

        /// Executes the given function synchronously without any exclusive tasks running, 
        /// returning its result as out parameter. 
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Returns true if the execution succeeded without throwing an exception, and false otherwise. 
        /// Throws an ArgumentNullException if the given function to execute is null.
        /// NOTE: may deadlock if the given function to execute calls this processing queue. 
        public bool QueueForExecution<T>(Func<T> execute, out T result)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            T res = default(T);
            var succeeded = true;
            this.QueueForExecutionAsync(() =>
            {
                try { res = execute(); }
                catch { succeeded = false; throw; } // will be caught as part of processing
            })
            .Wait();
            result = res;
            return succeeded;
        }


        // concurrent routines - i.e. routines that may execute at any time in between exclusive tasks

        /// Enqueues the given Action for concurrent (background) execution. 
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Throws an ArgumentNullException if the given Action is null.
        public Task ConcurrentExecutionAsync(Action processing)
        {
            if (processing == null) throw new ArgumentNullException(nameof(processing));
            var processingTask = ProcessingTaskAsync(processing);
            processingTask.Start(this.Scheduler.ConcurrentScheduler);
            return processingTask;
        }

        /// Executes the given Action synchronously and concurrently (background).
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Throws an ArgumentNullException if the given Action is null.
        /// NOTE: may deadlock if the given function to execute calls this processing queue. 
        public void ConcurrentExecution(Action processing)
        {
            if (processing == null) throw new ArgumentNullException(nameof(processing));
            this.ConcurrentExecution(() => { processing(); return new object(); }, out var _);
        }

        /// Executes the given function synchronously and concurrently, returning its result as out parameter.
        /// Returns true if the execution succeeded without throwing an exception, and false otherwise. 
        /// Uses the set exception logger to log any exception that occurs during execution. 
        /// Throws an ArgumentNullException if the given Action is null.
        /// NOTE: may deadlock if the given function to execute calls this processing queue. 
        public bool ConcurrentExecution<T>(Func<T> execute, out T result)
        {
            if (execute == null) throw new ArgumentNullException(nameof(execute));
            T res = default(T);
            var succeeded = true;
            this.ConcurrentExecutionAsync(() =>
            {
                try { res = execute(); }
                catch { succeeded = false; throw; } // will be caught as part of processing
            })
            .Wait();
            result = res;
            return succeeded;
        }
    }
}
