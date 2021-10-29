// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessServer : IOutOfProcessServer
    {
        private DateTime startTime;
        private Stopwatch idleStopwatch = new();
        private TelemetryManagerConfig configuration;
        private IOutOfProcessSerializer serializer;

        public OutOfProcessServer(TelemetryManagerConfig configuration)
        {
            this.serializer = (IOutOfProcessSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
            this.configuration = configuration;
        }

        private async IAsyncEnumerable<string> ConsoleReadLineAsync()
        {
            while (true)
            {
                var message = Console.ReadLine();
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
            this.serializer.Read(this.ConsoleReadLineAsync());

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
            while (this.idleStopwatch.Elapsed < this.configuration.OutOProcessMaxIdleTime)
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

                if (TelemetryManager.TestMode)
                {
                    return;
                }

                await Task.WhenAll(
                    this.QuitIfIdleAsync(),
                    this.ReceiveAndProcessCommandsAsync());
            }
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug(exception.ToString());
            }

            this.Quit();
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
            TelemetryManager.UploadNow();

            #if DEBUG
            var totalRunningTime = DateTime.Now - this.startTime;
            TelemetryManager.LogToDebug($"Exited. Total running time: {totalRunningTime:G})");
            #endif

            TelemetryManager.TearDown();

            // We don't want to exit from the process that is running the unit tests
            if (!TelemetryManager.TestMode)
            {
                Environment.Exit(0);
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