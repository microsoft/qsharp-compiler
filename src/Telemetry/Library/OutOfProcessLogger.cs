// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    internal enum OutOfProcessCommandType
    {
        SetContext,
        LogEvent,
        Quit,
    }

    internal class OutOfProcessCommand
    {
        public OutOfProcessCommand()
        {
        }

        public OutOfProcessCommand(OutOfProcessCommandType commandType, object? args)
        {
            this.CommandType = commandType;
            if (args != null)
            {
                this.Args = JsonSerializer.Serialize(args);
            }
        }

        public OutOfProcessCommandType CommandType { get; set; }

        public string? Args { get; set; }
    }

    internal class SetContextArgs
    {
        public SetContextArgs()
        {
        }

        public SetContextArgs(string name, object value, TelemetryPropertyType type, bool isPii)
        {
            this.Name = name;
            this.Value = value;
            this.PropertyType = type;
            this.IsPii = isPii;
        }

        public string? Name { get; set; }

        public TelemetryPropertyType PropertyType { get; set; }

        public object? Value { get; set; }

        public bool IsPii { get; set; }
    }

    internal class OutOfProcessServer
    {
        private static DateTime startTime;

        public static void RunAndExit(TelemetryManagerConfig configuration)
        {
            #if DEBUG
            startTime = DateTime.Now;
            #endif
            try
            {
                var idleStopwatch = Stopwatch.StartNew();
                var receiverThread = new Thread(() =>
                {
                    while (true)
                    {
                        var message = Console.ReadLine();
                        if (message != null)
                        {
                            ProcessCommand(message);
                            idleStopwatch.Restart();
                        }
                        else
                        {
                            Thread.Sleep(configuration.OutOfProcessPollWaitTime);
                        }
                    }
                });
                receiverThread.Start();

                while (idleStopwatch.Elapsed < configuration.OutOProcessMaxIdleTime)
                {
                    Thread.Sleep(configuration.OutOfProcessPollWaitTime);
                }
            }
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug(exception.ToString());
            }

            Quit();
        }

        private static void ProcessQuitCommand(string args) =>
            Quit();

        private static void ProcessLogEventCommand(string args)
        {
            var telemetryEvent = JsonSerializer.Deserialize<TelemetryEvent>(args);
            if (telemetryEvent != null)
            {
                foreach (var property in telemetryEvent.Properties.Values)
                {
                    if (property.Value != null)
                    {
                        property.Value = TypeConversionHelper.FromJsonElement((JsonElement)property.Value, property.PropertyType);
                    }
                }

                TelemetryManager.LogEvent(telemetryEvent.ToEventProperties());
            }
        }

        private static void ProcessSetContextCommand(string args)
        {
            var setContextArgs = JsonSerializer.Deserialize<SetContextArgs>(args);
            if (setContextArgs != null
                && setContextArgs.Name != null
                && setContextArgs.Value != null)
            {
                setContextArgs.Value = TypeConversionHelper.FromJsonElement((JsonElement)setContextArgs.Value, setContextArgs.PropertyType);
                TelemetryManager.SetContext(setContextArgs.Name, setContextArgs.Value, setContextArgs.PropertyType, setContextArgs.IsPii);
            }
        }

        private static Dictionary<OutOfProcessCommandType, Action<string>> processCommandActions = new()
        {
            { OutOfProcessCommandType.Quit, ProcessQuitCommand },
            { OutOfProcessCommandType.LogEvent, ProcessLogEventCommand },
            { OutOfProcessCommandType.SetContext, ProcessSetContextCommand },
        };

        private static void ProcessCommand(string message)
        {
            try
            {
                var command = JsonSerializer.Deserialize<OutOfProcessCommand>(message);
                if (command != null)
                {
                    processCommandActions[command.CommandType](command.Args ?? "");
                    TelemetryManager.LogToDebug($"OutOfProcess command processed: {command.CommandType}");
                }
            }
            catch (Exception exception)
            {
                TelemetryManager.LogToDebug($"Error at processing out of process command:{Environment.NewLine}{exception.ToString()}");
            }
        }

        private static void Quit()
        {
            TelemetryManager.UploadNow();

            #if DEBUG
            var totalRunningTime = DateTime.Now - startTime;
            TelemetryManager.LogToDebug($"Exited. Total running time: {totalRunningTime:G})");
            #endif

            TelemetryManager.TearDown();

            // We don't want to exit from the process that is running the unit tests
            if (!TelemetryManager.TestMode)
            {
                Environment.Exit(0);
            }
        }
    }

    public class OutOfProcessLogger : ILogger
    {
        private Process? externalProcess;
        private object instanceLock = new();
        #if DEBUG
        private Thread? debugThread;
        #endif

        public void AwaitForExternalProcessExit()
        {
            if (this.externalProcess != null && !this.externalProcess.HasExited)
            {
                this.externalProcess.WaitForExit();
            }
        }

        private void CreateExternalProcessIfNeeded()
        {
            if (this.externalProcess == null || this.externalProcess.HasExited)
            {
                lock (this.instanceLock)
                {
                    if (this.externalProcess == null || this.externalProcess.HasExited)
                    {
                        this.externalProcess = this.CreateExternalProcess();
                    }
                }
            }

            #if DEBUG
            if (this.debugThread == null || !this.debugThread.IsAlive)
            {
                lock (this.instanceLock)
                {
                    if (this.debugThread == null)
                    {
                        this.debugThread = this.CreateDebugThread();
                    }
                }
            }
            #endif
        }

        private Process? CreateExternalProcess()
        {
            var currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName;
            if (currentExecutablePath != null)
            {
                StringBuilder arguments = new();

                // if this is not a self-contained application, we need to run it via the dotnet cli
                if (string.Equals(
                                Path.GetFileNameWithoutExtension(currentExecutablePath),
                                "dotnet",
                                StringComparison.InvariantCultureIgnoreCase))
                {
                    arguments.Append($"run {Assembly.GetEntryAssembly()!.Location} ");
                }

                arguments.Append(TelemetryManager.OUTOFPROCESSUPLOADARG);

                return Process.Start(new ProcessStartInfo
                {
                    FileName = currentExecutablePath,
                    Arguments = arguments.ToString(),
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });
            }

            return null;
        }

        private EVTStatus SendCommand(OutOfProcessCommandType commandType, object? args = null)
        {
            // let's not start a new process just to ask it to quit
            if (commandType != OutOfProcessCommandType.Quit)
            {
                this.CreateExternalProcessIfNeeded();

                if (this.externalProcess == null || this.externalProcess.HasExited)
                {
                    throw new InvalidOperationException("OutOfProcessUpload external process is not running");
                }
            }

            if (this.externalProcess != null && !this.externalProcess.HasExited)
            {
                var command = new OutOfProcessCommand(commandType, args);
                var message = JsonSerializer.Serialize(command);
                lock (this.instanceLock)
                {
                    this.externalProcess.StandardInput.WriteLine(message);
                }
            }

            return EVTStatus.OK;
        }

        public void Quit() =>
            this.SendCommand(OutOfProcessCommandType.Quit);

        public EVTStatus LogEvent(EventProperties eventProperties) =>
            this.SendCommand(OutOfProcessCommandType.LogEvent, eventProperties.ToTelemetryEvent());

        public EVTStatus SetContext(string name, object value, TelemetryPropertyType type, PiiKind piiKind = PiiKind.None) =>
            this.SendCommand(OutOfProcessCommandType.SetContext, new SetContextArgs(name, value, type, piiKind != PiiKind.None));

        public EVTStatus SetContext(string name, string value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.String, piiKind);

        public EVTStatus SetContext(string name, double value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.Double, piiKind);

        public EVTStatus SetContext(string name, long value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.Long, piiKind);

        public EVTStatus SetContext(string name, bool value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.Boolean, piiKind);

        public EVTStatus SetContext(string name, DateTime value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.DateTime, piiKind);

        public EVTStatus SetContext(string name, Guid value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, value, TelemetryPropertyType.Guid, piiKind);

        public EVTStatus SetContext(string name, sbyte value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        public EVTStatus SetContext(string name, short value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        public EVTStatus SetContext(string name, int value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        public EVTStatus SetContext(string name, byte value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        public EVTStatus SetContext(string name, ushort value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        public EVTStatus SetContext(string name, uint value, PiiKind piiKind = PiiKind.None) =>
            this.SetContext(name, (long)value, piiKind);

        #if DEBUG
        private Thread CreateDebugThread()
        {
            var debugThread = new Thread(() =>
            {
                while (this.externalProcess != null && !this.externalProcess.HasExited)
                {
                    var message = this.externalProcess.StandardOutput.ReadLine();
                    if (message != null)
                    {
                        TelemetryManager.LogToDebug($"OutOfProcessUploader sent: {message}");
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }

                this.externalProcess = null;
                TelemetryManager.LogToDebug($"OutOfProcessUploader has exited.");
            });
            debugThread.Start();
            return debugThread;
        }
        #endif
    }
}
