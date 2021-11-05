// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Quantum.Telemetry.Commands;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessServer : ICommandProcessor
    {
        #if DEBUG
        private DateTime startTime;
        #endif
        private Stopwatch idleStopwatch = new Stopwatch();
        private TelemetryManagerConfig configuration;
        private ICommandSerializer serializer;
        private TextReader inputTextReader;
        private bool mustQuit = false;

        public OutOfProcessServer(TelemetryManagerConfig configuration, TextReader inputTextReader)
        {
            this.serializer = (ICommandSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
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

        private IAsyncEnumerable<CommandBase> ReceiveCommandsAsync() =>
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
            #if DEBUG
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug(exception.ToString());
            }
            #else
            catch
            {
            }
            #endif
        }

        public void RunAndExit() =>
            this.RunAndExitAsync().Wait();

        private void ProcessCommand(CommandBase command)
        {
            try
            {
                command.Process(this);
                #if DEBUG
                TelemetryManager.LogToDebug($"OutOfProcess command processed: {command.CommandType}");
                #endif
            }
            #if DEBUG
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug($"Error at processing out of process command:{Environment.NewLine}{exception.ToString()}");
            }
            #else
            catch
            {
            }
            #endif
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
                var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                if (!string.Equals("testhost", entryAssemblyName, StringComparison.InvariantCultureIgnoreCase))
                {
                    Environment.Exit(0);
                }
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
    }
}
