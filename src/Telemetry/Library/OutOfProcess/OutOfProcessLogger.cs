// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.Commands;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal interface IExternalProcessConnector
    {
        TextWriter? InputTextWriter { get; }

        TextReader? OutputTextReader { get; }

        bool IsRunning { get; }

        void WaitForExit();

        void Start();
    }

    internal class DefaultExternalProcessConnector : IExternalProcessConnector
    {
        private Process? process;

        public TextWriter? InputTextWriter => this.process?.StandardInput;

        public TextReader? OutputTextReader => this.process?.StandardOutput;

        public bool IsRunning => (this.process != null) && !this.process.HasExited;

        public void WaitForExit() => this.process?.WaitForExit();

        public void Start()
        {
            var currentExecutablePath = Process.GetCurrentProcess().MainModule!.FileName!;

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

            this.process = Process.Start(new ProcessStartInfo
            {
                FileName = currentExecutablePath,
                Arguments = arguments.ToString(),
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            if (this.process == null)
            {
                throw new InvalidOperationException($"Unable to start external process {currentExecutablePath} {arguments}");
            }
        }
    }

    internal class OutOfProcessLogger : ILogger
    {
        private IExternalProcessConnector externalProcess;
        private ICommandSerializer serializer;
        private TelemetryManagerConfig configuration;
        private object instanceLock = new();
        #if DEBUG
        private Thread? debugThread;
        #endif

        public OutOfProcessLogger(TelemetryManagerConfig configuration, IExternalProcessConnector? externalProcessConnector = null)
        {
            this.configuration = configuration;
            this.serializer = (ICommandSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
            this.externalProcess = externalProcessConnector ?? new DefaultExternalProcessConnector();
        }

        public void AwaitForExternalProcessExit() =>
            this.externalProcess.WaitForExit();

        private void CreateExternalProcessIfNeeded()
        {
            if (!this.externalProcess.IsRunning)
            {
                lock (this.instanceLock)
                {
                    if (!this.externalProcess.IsRunning)
                    {
                        this.externalProcess.Start();
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

        private EVTStatus SendCommand(CommandBase command)
        {
            // let's not start a new process just to ask it to quit
            if (!(command is QuitCommand))
            {
                this.CreateExternalProcessIfNeeded();

                if (!this.externalProcess.IsRunning)
                {
                    throw new InvalidOperationException("OutOfProcessUpload external process is not running");
                }
            }

            if (this.externalProcess.IsRunning)
            {
                var messages = this.serializer.Write(command);
                lock (this.instanceLock)
                {
                    foreach (var message in messages)
                    {
                        this.externalProcess.InputTextWriter?.WriteLine(message);
                    }
                }
            }

            return EVTStatus.OK;
        }

        public void Quit() =>
            this.SendCommand(new QuitCommand());

        public EVTStatus LogEvent(EventProperties eventProperties) =>
            this.SendCommand(new LogEventCommand(eventProperties));

        public EVTStatus SetContext(string name, object value, TelemetryPropertyType type, PiiKind piiKind = PiiKind.None) =>
            this.SendCommand(new SetContextCommand(new SetContextArgs(name, value, type, piiKind != PiiKind.None)));

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
                while (this.externalProcess.OutputTextReader != null)
                {
                    var message = this.externalProcess.OutputTextReader.ReadLine();
                    if (message != null)
                    {
                        TelemetryManager.LogToDebug($"OutOfProcessUploader sent: {message}");
                    }
                    else
                    {
                        Thread.Sleep(TimeSpan.FromSeconds(1));

                        if (!this.externalProcess.IsRunning)
                        {
                            break;
                        }
                    }
                }

                TelemetryManager.LogToDebug($"OutOfProcessUploader has exited.");
            });
            debugThread.Start();
            return debugThread;
        }
        #endif
    }
}
