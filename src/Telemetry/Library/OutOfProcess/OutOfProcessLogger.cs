// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Applications.Events;
using Microsoft.Quantum.Telemetry.Commands;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    public interface IExternalProcessConnector
    {
        TextWriter? InputTextWriter { get; }

        TextReader? OutputTextReader { get; }

        bool IsRunning { get; }

        void WaitForExit();

        void Start();

        void Kill();
    }

    internal class DefaultExternalProcessConnector : IExternalProcessConnector
    {
        private TelemetryManagerConfig configuration;

        private Process? process;

        public TextWriter? InputTextWriter => this.process?.StandardInput;

        public TextReader? OutputTextReader => this.process?.StandardOutput;

        public bool IsRunning => (this.process != null) && !this.process.HasExited;

        public void WaitForExit() => this.process?.WaitForExit();

        public DefaultExternalProcessConnector(TelemetryManagerConfig configuration)
        {
            this.configuration = configuration;
        }

        public void Start()
        {
            List<string> arguments = new List<string>();

            var outOfProcessExecutablePath = this.configuration.OutOfProcessExecutablePath;

            if (string.IsNullOrEmpty(outOfProcessExecutablePath))
            {
                outOfProcessExecutablePath = Process.GetCurrentProcess().MainModule!.FileName!;

                // if this is not a self-contained application, we need to run it via the dotnet cli
                if (string.Equals(
                                Path.GetFileNameWithoutExtension(outOfProcessExecutablePath),
                                "dotnet",
                                StringComparison.InvariantCultureIgnoreCase))
                {
                    var entryAssemblyName = Assembly.GetEntryAssembly()?.GetName().Name;
                    if (string.Equals("testhost", entryAssemblyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new InvalidOperationException($"'testhost' cannot be used as an external process. Please set the config option 'OutOfProcessExecutablePath'.");
                    }
                    else
                    {
                        arguments.Add($"{Assembly.GetEntryAssembly()!.Location} ");
                    }
                }
            }
            else if (string.Equals(
                                ".dll",
                                Path.GetExtension(outOfProcessExecutablePath),
                                StringComparison.InvariantCultureIgnoreCase))
            {
                // if this is not a self-contained application, we need to run it via the dotnet cli
                arguments.Add(outOfProcessExecutablePath);
                outOfProcessExecutablePath = "dotnet";
            }

            arguments.Add(TelemetryManager.OUTOFPROCESSUPLOADARG);

            if (this.configuration.TestMode)
            {
                arguments.Add(TelemetryManager.TESTMODE);
            }

            this.process = Process.Start(new ProcessStartInfo
            {
                FileName = outOfProcessExecutablePath,
                Arguments = string.Join(' ', arguments),
                RedirectStandardInput = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            });

            if (this.process == null)
            {
                throw new InvalidOperationException($"Unable to start external process {outOfProcessExecutablePath} {arguments}");
            }
        }

        public void Kill()
        {
            this.process?.Kill();
            this.process = null;
        }
    }

    public class OutOfProcessLogger : ILogger, IDisposable
    {
        private IExternalProcessConnector externalProcess;
        private ICommandSerializer serializer;
        private TelemetryManagerConfig configuration;
        private object instanceLock = new object();
        private bool disposedValue;

        public OutOfProcessLogger(TelemetryManagerConfig configuration, IExternalProcessConnector? externalProcessConnector = null)
        {
            this.configuration = configuration;
            this.serializer = (ICommandSerializer)Activator.CreateInstance(configuration.OutOfProcessSerializerType)!;
            this.externalProcess = externalProcessConnector ?? new DefaultExternalProcessConnector(configuration);
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

                        if (TelemetryManager.DebugMode || TelemetryManager.TestMode)
                        {
                            this.CreateDebugThread();
                        }
                    }
                }
            }
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

            var success = this.TrySendCommand(command);

            // try one more time if this is not a Quit command
            if (!success && !(command is QuitCommand))
            {
                this.CreateExternalProcessIfNeeded();
                success = this.TrySendCommand(command);
            }

            return success ? EVTStatus.OK : EVTStatus.Fail;
        }

        private bool TrySendCommand(CommandBase command)
        {
            try
            {
                var messages = this.serializer.Write(command);
                lock (this.instanceLock)
                {
                    foreach (var message in messages)
                    {
                        this.externalProcess.InputTextWriter?.WriteLine(message);
                    }
                }

                return true;
            }
            catch (IOException)
            {
                // If we get an IOException it means we are unable to communicate
                // with the external process anymore and we need to kill it.
                this.externalProcess.Kill();
            }

            return false;
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

        private void CreateDebugThread() =>
            new Thread(this.DebugThreadMethod).Start();

        private void DebugThreadMethod()
        {
            while (!this.disposedValue
                   && this.externalProcess.OutputTextReader != null)
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
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                this.disposedValue = true;
            }
        }

        public void Dispose()
        {
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
