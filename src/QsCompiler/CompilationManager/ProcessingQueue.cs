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
        /// Header string for the message given when an exception is logged.
        /// </summary>
        private readonly string exceptionHeader;

        /// <summary>
        /// Used to log exceptions raised during processing.
        /// </summary>
        private readonly Action<Exception> logException;

        /// <summary>
        /// Schedulers both for tasks that have to run sequentially and for tasks that can run simultaneously.
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
        /// Enqueues <paramref name="processing"/> for exclusive (serialized) execution.
        /// </summary>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// </remarks>
        public Task QueueForExecutionAsync(Action processing)
        {
            var processingTask = this.ProcessingTaskAsync(processing);
            processingTask.Start(this.scheduler.ExclusiveScheduler);
            return processingTask;
        }

        /// <summary>
        /// Executes <paramref name="processing"/> synchronously, with no exclusive actions running.
        /// </summary>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// <para/>
        /// NOTE: may deadlock if <paramref name="processing"/> calls this processing queue.
        /// </remarks>
        public void QueueForExecution(Action processing) =>
            this.QueueForExecution(
                () =>
                {
                    processing();
                    return new object();
                },
                out _);

        /// <summary>
        /// Executes <paramref name="execute"/> synchronously without any exclusive tasks running,
        /// returning its result via <paramref name="result"/>.
        /// </summary>
        /// <returns>
        /// True if the execution succeeded without throwing an exception, and false otherwise.
        /// </returns>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// <para/>
        /// NOTE: may deadlock if <paramref name="execute"/> calls this processing queue.
        /// </remarks>
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
        /// Enqueues <paramref name="processing"/> for concurrent (background) execution.
        /// </summary>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// </remarks>
        public Task ConcurrentExecutionAsync(Action processing)
        {
            var processingTask = this.ProcessingTaskAsync(processing);
            processingTask.Start(this.scheduler.ConcurrentScheduler);
            return processingTask;
        }

        /// <summary>
        /// Executes <paramref name="processing"/> synchronously and concurrently (background).
        /// </summary>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// <para/>
        /// NOTE: may deadlock if <paramref name="processing"/> calls this processing queue.
        /// </remarks>
        public void ConcurrentExecution(Action processing) =>
            this.ConcurrentExecution(
                () =>
                {
                    processing();
                    return new object();
                },
                out _);

        /// <summary>
        /// Executes <paramref name="execute"/> synchronously and concurrently, returning its result via <paramref name="result"/>.
        /// </summary>
        /// <returns>
        /// True if the execution succeeded without throwing an exception, and false otherwise.
        /// </returns>
        /// <remarks>
        /// Uses the set exception logger to log any exception that occurs during execution.
        /// <para/>
        /// NOTE: may deadlock if <paramref name="execute"/> calls this processing queue.
        /// </remarks>
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
