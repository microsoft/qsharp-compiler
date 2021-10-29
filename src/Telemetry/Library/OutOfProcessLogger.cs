// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry
{
    public enum OutOfProcessCommandType
    {
        SetContext,
        LogEvent,
        Quit,
    }

    public abstract class OutOfProcessCommand
    {
        public OutOfProcessCommand(OutOfProcessCommandType commandType, object? args = null)
        {
            this.CommandType = commandType;
            this.Args = args;
        }

        public OutOfProcessCommandType CommandType { get; set; }

        public object? Args { get; set; }

        public abstract void Process(IOutOfProcessServer server);
    }

    public class OutOfProcessQuitCommand : OutOfProcessCommand
    {
        public OutOfProcessQuitCommand()
            : base(OutOfProcessCommandType.Quit)
        {
        }

        public override bool Equals(object? obj) =>
             obj is OutOfProcessQuitCommand;

        public override int GetHashCode() =>
            this.CommandType.GetHashCode();

        public override void Process(IOutOfProcessServer server) =>
            server.ProcessCommand(this);
     }

    public class OutOfProcessSetContextCommand : OutOfProcessCommand
    {
        public OutOfProcessSetContextCommand(SetContextArgs? setContextArgs = null)
            : base(OutOfProcessCommandType.SetContext, setContextArgs ?? new())
        {
        }

        public new SetContextArgs Args
        {
            get => (SetContextArgs)base.Args!;
            set => base.Args = value;
        }

        public override bool Equals(object? obj) =>
            (obj is OutOfProcessSetContextCommand setContextCommand)
            && object.Equals(this.Args, setContextCommand.Args);

        public override int GetHashCode() =>
            this.CommandType.GetHashCode() ^ this.Args.GetHashCode();

        public override void Process(IOutOfProcessServer server) =>
            server.ProcessCommand(this);
    }

    public class OutOfProcessLogEventCommand : OutOfProcessCommand
    {
        public OutOfProcessLogEventCommand(EventProperties? eventProperties = null)
            : base(OutOfProcessCommandType.LogEvent, eventProperties ?? new())
        {
        }

        public new EventProperties Args
        {
            get => (EventProperties)base.Args!;
            set => base.Args = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is OutOfProcessLogEventCommand logEventCommand)
            {
                if (logEventCommand.Args.Name != this.Args.Name)
                {
                    return false;
                }

                foreach (var property in this.Args.Properties)
                {
                    var isPii = this.Args.PiiProperties.ContainsKey(property.Key);
                    if (!logEventCommand.Args.Properties.TryGetValue(property.Key, out var value)
                        || !object.Equals(value, property.Value)
                        || (isPii && !logEventCommand.Args.PiiProperties.ContainsKey(property.Key)))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        public override int GetHashCode() =>
            this.CommandType.GetHashCode() ^ this.Args.GetHashCode();

        public override void Process(IOutOfProcessServer server) =>
            server.ProcessCommand(this);
    }

    public class SetContextArgs
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

        public override bool Equals(object? obj) =>
            (obj is SetContextArgs setContextArgs)
            && object.Equals(setContextArgs.IsPii, this.IsPii)
            && object.Equals(setContextArgs.Name, this.Name)
            && object.Equals(setContextArgs.PropertyType, this.PropertyType)
            && object.Equals(setContextArgs.Value, this.Value);

        public override int GetHashCode() =>
            (this.Name?.GetHashCode() ?? 1)
            ^ this.PropertyType.GetHashCode()
            ^ (this.Value?.GetHashCode() ?? 1);
    }

    public interface IOutOfProcessSerializer
    {
        IEnumerable<string> Write(IEnumerable<OutOfProcessCommand> commands);

        IEnumerable<string> Write(OutOfProcessCommand command);

        IAsyncEnumerable<OutOfProcessCommand> Read(IAsyncEnumerable<string> messages);
    }

    public class SimpleYamlSerializer : IOutOfProcessSerializer
    {
        private static readonly string LineBreak = @"__\r\n__";
        private static readonly string EventNamePropertyName = "__name__";
        private static readonly Regex CommandRegex = new(@"^((?<command>- command:\s+!(?<commandType>[^\s$]+).*$)|(?<property>\s{4}(?<key>[^\s:]+):\s*(!(?<type>[^\s:\+]+)(?<pii>\+Pii)?\s+)?(?<value>.+)))$", RegexOptions.Compiled);
        private static readonly Regex EscapeLineBreaksRegex = new(@"(\r\n|\n\r|\n|\r)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ReplaceLineBreaksRegex = new(LineBreak.Replace(@"\", @"\\"), RegexOptions.Compiled);

        private static string PiiType(bool isPii) =>
            isPii ? "+Pii" : "";

        private static string PiiType(PiiKind piiKind) =>
            PiiType(piiKind != PiiKind.None);

        private static string PropertyToYamlString(string name, TelemetryPropertyType type, object? value, bool isPii) =>
            $"    {name}: !{type}{PiiType(isPii)} {EscapeLineBreaks(value)}";

        private static string EscapeLineBreaks(object? value)
        {
            if (value is string text)
            {
                return EscapeLineBreaksRegex.Replace(text, LineBreak);
            }

            return $"{value}";
        }

        private static string ReplaceLineBreaks(string text) =>
            ReplaceLineBreaksRegex.Replace(text, Environment.NewLine);

        private static string SetContextArgsToYamlString(SetContextArgs args) =>
            PropertyToYamlString(
                name: args.Name!,
                type: args.PropertyType,
                value: args.Value,
                isPii: args.IsPii);

        private static string EventPropertyToYamlString(KeyValuePair<string, object> property, bool isPii) =>
            PropertyToYamlString(
                name: property.Key,
                type: TypeConversionHelper.TypeMap[property.Value.GetType()],
                value: property.Value,
                isPii: isPii);

        public IEnumerable<string> Write(IEnumerable<OutOfProcessCommand> commands) =>
            commands.SelectMany((command) => this.Write(command));

        public IEnumerable<string> Write(OutOfProcessCommand command)
        {
            yield return $"- command: !{command.CommandType}";

            if (command is OutOfProcessSetContextCommand setContextCommand)
            {
                yield return SetContextArgsToYamlString(setContextCommand.Args);
            }
            else if (command is OutOfProcessLogEventCommand logEventCommand)
            {
                var eventProperties = logEventCommand.Args;
                yield return PropertyToYamlString(
                                name: EventNamePropertyName,
                                type: TelemetryPropertyType.String,
                                value: eventProperties.Name,
                                isPii: false);
                foreach (var property in eventProperties.Properties)
                {
                    var isPii = eventProperties.PiiProperties.ContainsKey(property.Key);
                    yield return EventPropertyToYamlString(property, isPii);
                }
            }

            yield return "";
        }

        public async IAsyncEnumerable<OutOfProcessCommand> Read(IAsyncEnumerable<string> messages)
        {
            var enumerator = messages.GetAsyncEnumerator();
            OutOfProcessCommand? command = null;

            while (await enumerator.MoveNextAsync())
            {
                var line = enumerator.Current;
                if (line == "")
                {
                    if (command != null)
                    {
                        yield return command;
                        command = null;
                    }

                    continue;
                }

                var match = CommandRegex.Match(line);
                if (match?.Success == true)
                {
                    if (match.Groups["commandType"].Success)
                    {
                        if (command != null)
                        {
                            yield return command;
                            command = null;
                        }

                        var commandType = match.Groups["commandType"].Value;
                        switch (commandType)
                        {
                            case nameof(OutOfProcessCommandType.LogEvent):
                                command = new OutOfProcessLogEventCommand();
                                break;
                            case nameof(OutOfProcessCommandType.Quit):
                                yield return new OutOfProcessQuitCommand();
                                break;
                            case nameof(OutOfProcessCommandType.SetContext):
                                command = new OutOfProcessSetContextCommand();
                                break;
                            default:
                                TelemetryManager.LogToDebug($"Unexpected YAML commandType: {commandType}");
                                break;
                        }
                    }
                    else if (match.Groups["property"].Success)
                    {
                        var key = match.Groups["key"].Value;
                        var type = match.Groups["type"].Value;
                        var isPii = match.Groups["pii"].Success;
                        var value = match.Groups["value"].Value.Trim(' ', '"');
                        var piiKind = isPii ? PiiKind.GenericData : PiiKind.None;

                        if (type == "")
                        {
                            type = nameof(TelemetryPropertyType.String);
                        }

                        if (command is OutOfProcessLogEventCommand logEventCommand)
                        {
                            SetLogEventProperty(logEventCommand, key, type, value, piiKind);
                        }
                        else if (command is OutOfProcessSetContextCommand setContextCommand)
                        {
                            var propertyType = Enum.Parse<TelemetryPropertyType>(type);
                            setContextCommand.Args.Name = key;
                            setContextCommand.Args.PropertyType = propertyType;
                            setContextCommand.Args.IsPii = isPii;
                            setContextCommand.Args.Value = ConvertValueFromString(value, propertyType);
                        }
                    }
                }
                else
                {
                    TelemetryManager.LogToDebug($"Unexpected YAML string: {line}");
                }
            }

            if (command != null)
            {
                yield return command;
            }
        }

        private static object ConvertValueFromString(string value, TelemetryPropertyType propertyType)
        {
            switch (propertyType)
            {
                case TelemetryPropertyType.Boolean:
                    return bool.Parse(value);
                case TelemetryPropertyType.DateTime:
                    return DateTime.Parse(value);
                case TelemetryPropertyType.Double:
                    return double.Parse(value);
                case TelemetryPropertyType.Guid:
                    return Guid.Parse(value);
                case TelemetryPropertyType.Long:
                    return long.Parse(value);
                case TelemetryPropertyType.String:
                    return ReplaceLineBreaks(value);
                default:
                    throw new ArgumentOutOfRangeException(message: $"{propertyType} conversion not implemented", innerException: null);
            }
        }

        private static void SetLogEventProperty(OutOfProcessLogEventCommand logEventCommand, string key, string type, string value, PiiKind piiKind)
        {
            switch (type)
            {
                case nameof(TelemetryPropertyType.Boolean):
                    logEventCommand.Args.SetProperty(key, bool.Parse(value), piiKind);
                    break;
                case nameof(TelemetryPropertyType.DateTime):
                    logEventCommand.Args.SetProperty(key, DateTime.Parse(value), piiKind);
                    break;
                case nameof(TelemetryPropertyType.Double):
                    logEventCommand.Args.SetProperty(key, double.Parse(value), piiKind);
                    break;
                case nameof(TelemetryPropertyType.Guid):
                    logEventCommand.Args.SetProperty(key, Guid.Parse(value), piiKind);
                    break;
                case nameof(TelemetryPropertyType.Long):
                    logEventCommand.Args.SetProperty(key, long.Parse(value), piiKind);
                    break;
                case nameof(TelemetryPropertyType.String):
                    if (key == EventNamePropertyName)
                    {
                        logEventCommand.Args.Name = value;
                    }
                    else
                    {
                        logEventCommand.Args.SetProperty(key, ReplaceLineBreaks(value), piiKind);
                    }

                    break;
                default:
                    TelemetryManager.LogToDebug($"Unexpected YAML type: {type}");
                    break;
            }
        }

        public void Write(StreamWriter streamWriter, OutOfProcessCommand command)
        {
            throw new NotImplementedException();
        }
    }

    public interface IOutOfProcessServer
    {
        void ProcessCommand(OutOfProcessQuitCommand command);

        void ProcessCommand(OutOfProcessLogEventCommand command);

        void ProcessCommand(OutOfProcessSetContextCommand command);
    }

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

    public class OutOfProcessLogger : ILogger
    {
        private Process? externalProcess;
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
                        this.externalProcess.StandardInput.WriteLine(message);
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
