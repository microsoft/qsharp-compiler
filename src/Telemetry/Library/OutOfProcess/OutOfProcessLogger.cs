// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessLogger : ILogger
    {
        internal interface IExternalProcess
        {
            StreamWriter In { get; }

            StreamReader Out { get; }

            bool HasExited { get; }

            void WaitForExit();
        }

        internal class ExternalProcess : IExternalProcess
        {
            private Process process;

            private ExternalProcess(Process process)
            {
                this.process = process;
            }

            public StreamWriter In => this.process.StandardInput;

            public StreamReader Out => this.process.StandardOutput;

            public bool HasExited => this.process.HasExited;

            public void WaitForExit() => this.process.WaitForExit();

            public static IExternalProcess StartNew(string fileName, string arguments)
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                });

                if (process == null)
                {
                    throw new InvalidOperationException($"Unable to start external process {fileName} {arguments}");
                }

                return new ExternalProcess(process);
            }
        }

        private IExternalProcess? externalProcess;
        private IOutOfProcessSerializer serializer;
        private TelemetryManagerConfig configuration;
        private object instanceLock = new();
        #if DEBUG
        private Thread? debugThread;
        #endif

        public OutOfProcessLogger(TelemetryManagerConfig configuration)
        {
            this.configuration = configuration;
            this.serializer = (IOutOfProcessSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
        }

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

        private IExternalProcess? CreateExternalProcess()
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

            return ExternalProcess.StartNew(currentExecutablePath, arguments.ToString());
        }

        private EVTStatus SendCommand(OutOfProcessCommand command)
        {
            // let's not start a new process just to ask it to quit
            if (!(command is OutOfProcessQuitCommand))
            {
                this.CreateExternalProcessIfNeeded();

                if (this.externalProcess == null || this.externalProcess.HasExited)
                {
                    throw new InvalidOperationException("OutOfProcessUpload external process is not running");
                }
            }

            if (this.externalProcess != null && !this.externalProcess.HasExited)
            {
                var messages = this.serializer.Write(command);
                lock (this.instanceLock)
                {
                    foreach (var message in messages)
                    {
                        this.externalProcess.In.WriteLine(message);
                    }
                }
            }

            return EVTStatus.OK;
        }

        public void Quit() =>
            this.SendCommand(new OutOfProcessQuitCommand());

        public EVTStatus LogEvent(EventProperties eventProperties) =>
            this.SendCommand(new OutOfProcessLogEventCommand(eventProperties));

        public EVTStatus SetContext(string name, object value, TelemetryPropertyType type, PiiKind piiKind = PiiKind.None) =>
            this.SendCommand(new OutOfProcessSetContextCommand(new SetContextArgs(name, value, type, piiKind != PiiKind.None)));

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
                    var message = this.externalProcess.Out.ReadLine();
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
