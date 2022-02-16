// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Quantum.Telemetry.Commands;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    public class OutOfProcessServer : ICommandProcessor, IDisposable
    {
        private DateTime startTime;
        private Stopwatch idleStopwatch = new Stopwatch();
        private TelemetryManagerConfig configuration;
        private ICommandSerializer serializer;
        private TextReader inputTextReader;
        private Thread? quitIfIdleThread;
        private bool isDisposed;

        public OutOfProcessServer(TelemetryManagerConfig configuration, TextReader inputTextReader)
        {
            this.serializer = (ICommandSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
            this.configuration = configuration;
            this.inputTextReader = inputTextReader;
        }

        private IEnumerable<string> ReadInputLine()
        {
            while (!this.isDisposed)
            {
                var message = this.inputTextReader.ReadLine();
                if (message != null)
                {
                    yield return message;
                }
                else
                {
                    Thread.Sleep(this.configuration.OutOfProcessPollWaitTime);
                }
            }
        }

        private IEnumerable<CommandBase> ReceiveCommands() =>
            this.serializer.Read(this.ReadInputLine());

        private void ReceiveAndProcessCommands()
        {
            foreach (var command in this.ReceiveCommands())
            {
                this.ProcessCommand(command);
                this.idleStopwatch.Restart();
            }
        }

        private void QuitIfIdleLoop()
        {
            try
            {
                while (!this.isDisposed
                    && (this.idleStopwatch.Elapsed < this.configuration.OutOfProcessMaxIdleTime))
                {
                    Thread.Sleep(this.configuration.OutOfProcessPollWaitTime);
                }
            }
            catch (ThreadInterruptedException)
            {
                return;
            }

            if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
            {
                TelemetryManager.LogToDebug($"Quitting OutOfProcess server due to innactivity ({this.configuration.OutOfProcessMaxIdleTime}).");
            }

            this.Quit();
        }

        public void RunAndExit()
        {
            this.startTime = DateTime.Now;

            try
            {
                this.idleStopwatch.Restart();

                if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
                {
                    this.quitIfIdleThread = new Thread(this.QuitIfIdleLoop);
                    this.quitIfIdleThread.Priority = ThreadPriority.Lowest;
                    this.quitIfIdleThread.Start();
                }

                this.ReceiveAndProcessCommands();
            }
            catch (Exception exception)
            {
                if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
                {
                    TelemetryManager.LogToDebug(exception.ToString());
                }
            }
            finally
            {
                this.Quit();
            }
        }

        private void ProcessCommand(CommandBase command)
        {
            try
            {
                command.Process(this);
                if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
                {
                    TelemetryManager.LogToDebug($"OutOfProcess command processed: {command.CommandType}");
                }
            }
            catch (Exception exception)
            {
                if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
                {
                    TelemetryManager.LogToDebug($"Error at processing out of process command: {command.CommandType}{Environment.NewLine}{exception.ToString()}");

                    // In test mode, we can throw an OutOfProcessTestException to simulate an
                    // unhandled exception in the OutOfProcess server
                    // See Test.OutProcessExe.Program class.
                    if ("OutOfProcessTestException".Equals(exception.GetType().Name))
                    {
                        throw;
                    }
                }
            }
        }

        private void Quit()
        {
            this.Dispose();

            // We don't want to exit from the process that is running the unit tests
            if (!TelemetryManager.IsTestHostProcess)
            {
                Environment.Exit(0);
            }
        }

        public void ProcessCommand(QuitCommand command) =>
            this.Quit();

        public void ProcessCommand(LogEventCommand command) =>
            TelemetryManager.LogEvent(command.Args);

        public void ProcessCommand(SetContextCommand command) =>
            TelemetryManager.SetContext(
                command.Args.Name!,
                command.Args.Value!,
                command.Args.PropertyType,
                command.Args.IsPii);

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    TelemetryManager.UploadNow();

                    if (TelemetryManager.TestMode || TelemetryManagerConstants.IsDebugBuild)
                    {
                        var totalRunningTime = DateTime.Now - this.startTime;
                        TelemetryManager.LogToDebug($"Exited. Total running time: {totalRunningTime:G})");
                    }

                    TelemetryManager.TearDown();
                }

                this.isDisposed = true;

                this.quitIfIdleThread?.Interrupt();
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
