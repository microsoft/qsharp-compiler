// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// Class allowing to enforce in-order processing of tasks.
    /// </summary>
    public class ProcessingQueue
    {
        /// <summary>
        /// header string for the message given when an exception if logged
        /// </summary>
        private readonly string exceptionHeader;

        /// <summary>
        /// used to log exceptions raised during processing
        /// </summary>
        private readonly Action<Exception> logException;

        /// <summary>
        /// schedulers both for tasks that have to run sequentially and for tasks that can run simultaneously
        /// </summary>
        private readonly ConcurrentExclusiveSchedulerPair scheduler;

        public ProcessingQueue(Action<Exception>? exceptionLogger, string? exceptionHeader = null)
        {
            this.exceptionHeader = exceptionHeader ?? "error while running queued task";
            this.logException = exceptionLogger ?? Console.Error.WriteLine;
            this.scheduler = new ConcurrentExclusiveSchedulerPair();
        }

        private Task ProcessingTaskAsync(Action action) =>
            new Task(() =>
            {
                try
                {
                    QsCompilerError.RaiseOnFailure(action, this.exceptionHeader);
                }
                catch (Exception ex)
                {
                    this.logException(ex);
                }
            });

        /// <summary>
        /// The queue will not accept any more tasks after calling complete.
        /// </summary>
        public void Complete() =>
            this.scheduler.Complete();

        // non-concurrent routines - i.e. routines that are forced to execute in order

        /// <summary>
        /// Enqueues the given Action for exclusive (serialized) execution.
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// </summary>
        public Task QueueForExecutionAsync(Action processing)
        {
            var processingTask = this.ProcessingTaskAsync(processing);
            processingTask.Start(this.scheduler.ExclusiveScheduler);
            return processingTask;
        }

        /// <summary>
        /// Executes the given Action synchronously, with no exclusive actions running.
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// NOTE: may deadlock if the given function to execute calls this processing queue.
        /// </summary>
        public void QueueForExecution(Action processing) =>
            this.QueueForExecution(
                () =>
                {
                    processing();
                    return new object();
                },
                out _);

        /// <summary>
        /// Executes the given function synchronously without any exclusive tasks running,
        /// returning its result as out parameter.
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// Returns true if the execution succeeded without throwing an exception, and false otherwise.
        /// NOTE: may deadlock if the given function to execute calls this processing queue.
        /// </summary>
        public bool QueueForExecution<T>(Func<T> execute, [MaybeNull] out T result)
        {
            T res = default(T);
            var succeeded = true;
            this.QueueForExecutionAsync(() =>
            {
                try
                {
                    res = execute();
                }
                catch
                {
                    // will be caught as part of processing
                    succeeded = false;
                    throw;
                }
            })
            .Wait();
            result = res;
            return succeeded;
        }

        // concurrent routines - i.e. routines that may execute at any time in between exclusive tasks

        /// <summary>
        /// Enqueues the given Action for concurrent (background) execution.
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// </summary>
        public Task ConcurrentExecutionAsync(Action processing)
        {
            var processingTask = this.ProcessingTaskAsync(processing);
            processingTask.Start(this.scheduler.ConcurrentScheduler);
            return processingTask;
        }

        /// <summary>
        /// Executes the given Action synchronously and concurrently (background).
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// NOTE: may deadlock if the given function to execute calls this processing queue.
        /// </summary>
        public void ConcurrentExecution(Action processing) =>
            this.ConcurrentExecution(
                () =>
                {
                    processing();
                    return new object();
                },
                out _);

        /// <summary>
        /// Executes the given function synchronously and concurrently, returning its result as out parameter.
        /// Returns true if the execution succeeded without throwing an exception, and false otherwise.
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// NOTE: may deadlock if the given function to execute calls this processing queue.
        /// </summary>
        public bool ConcurrentExecution<T>(Func<T> execute, [MaybeNull] out T result)
        {
            T res = default(T);
            var succeeded = true;
            this.ConcurrentExecutionAsync(() =>
            {
                try
                {
                    res = execute();
                }
                catch
                {
                    // will be caught as part of processing
                    succeeded = false;
                    throw;
                }
            })
            .Wait();
            result = res;
            return succeeded;
        }
    }
}
