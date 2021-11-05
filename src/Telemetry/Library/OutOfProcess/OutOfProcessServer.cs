// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessServer : IOutOfProcessServer
    {
        private DateTime startTime;
        private Stopwatch idleStopwatch = new();
        private TelemetryManagerConfig configuration;
        private IOutOfProcessSerializer serializer;
        private TextReader inputTextReader;
        private bool mustQuit = false;

        public OutOfProcessServer(TelemetryManagerConfig configuration, TextReader inputTextReader)
        {
            this.serializer = (IOutOfProcessSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
            this.configuration = configuration;
            this.inputTextReader = inputTextReader;
        }

        private async IAsyncEnumerable<string> ReadInputLineAsync()
        {
            while (!this.mustQuit)
            {
                var message = this.inputTextReader.ReadLine();
                if (message != null)
                {
                    yield return message;
                }
                else
                {
                    await Task.Delay(this.configuration.OutOfProcessPollWaitTime);
                }
            }
        }

        private IAsyncEnumerable<OutOfProcessCommand> ReceiveCommandsAsync() =>
            this.serializer.Read(this.ReadInputLineAsync());

        private async Task ReceiveAndProcessCommandsAsync()
        {
            await foreach (var command in this.ReceiveCommandsAsync())
            {
                this.ProcessCommand(command);
                this.idleStopwatch.Restart();
            }
        }

        private async Task QuitIfIdleAsync()
        {
            while (!this.mustQuit
                   && (this.idleStopwatch.Elapsed < this.configuration.OutOfProcessMaxIdleTime))
            {
                await Task.Delay(this.configuration.OutOfProcessPollWaitTime);
            }

            this.Quit();
        }

        public async Task RunAndExitAsync()
        {
            #if DEBUG
            this.startTime = DateTime.Now;
            #endif
            try
            {
                this.idleStopwatch.Restart();

                await Task.WhenAll(
                    this.QuitIfIdleAsync(),
                    this.ReceiveAndProcessCommandsAsync());
            }
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug(exception.ToString());
            }
        }

        public void RunAndExit() =>
            this.RunAndExitAsync().Wait();

        private void ProcessCommand(OutOfProcessCommand command)
        {
            try
            {
                command.Process(this);
                TelemetryManager.LogToDebug($"OutOfProcess command processed: {command.CommandType}");
            }
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug($"Error at processing out of process command:{Environment.NewLine}{exception.ToString()}");
            }
        }

        private void Quit()
        {
            if (!this.mustQuit)
            {
                TelemetryManager.UploadNow();

                #if DEBUG
                var totalRunningTime = DateTime.Now - this.startTime;
                TelemetryManager.LogToDebug($"Exited. Total running time: {totalRunningTime:G})");
                #endif

                TelemetryManager.TearDown();

                this.mustQuit = true;

                // We don't want to exit from the process that is running the unit tests
                if (!TelemetryManager.TestMode)
                {
                    Environment.Exit(0);
                }
            }
        }

        public void ProcessCommand(OutOfProcessQuitCommand command) =>
            this.Quit();

        public void ProcessCommand(OutOfProcessLogEventCommand command) =>
            TelemetryManager.LogEvent(command.Args);

        public void ProcessCommand(OutOfProcessSetContextCommand command) =>
            TelemetryManager.SetContext(
                command.Args.Name!,
                command.Args.Value!,
                command.Args.PropertyType,
                command.Args.IsPii);
    }
}