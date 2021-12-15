// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Quantum.Telemetry.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 1, Scope = ExecutionScope.MethodLevel)]

namespace Microsoft.Quantum.Telemetry.Tests
{
    public class TestCommon
    {
        internal class NullOutOfProcessServer : ICommandProcessor
        {
            public CommandBase? LastProcessedCommand { get; private set; }

            public Type? LastProcessedType { get; private set; }

            private void InternalProcess<T>(T command)
                where T : CommandBase
            {
                this.LastProcessedCommand = command;
                this.LastProcessedType = typeof(T);
            }

            public void ProcessCommand(QuitCommand command) =>
                this.InternalProcess(command);

            public void ProcessCommand(LogEventCommand command) =>
                this.InternalProcess(command);

            public void ProcessCommand(SetContextCommand command) =>
                this.InternalProcess(command);
        }

        internal static NullOutOfProcessServer CreateNullOutOfProcessServer() => new NullOutOfProcessServer();

        internal static QuitCommand CreateQuitCommand() => new QuitCommand();

        internal static Applications.Events.EventProperties CreateEventProperties(int seed)
        {
            Applications.Events.EventProperties eventProperties = new Applications.Events.EventProperties();
            eventProperties.Name = $"eventName{seed}";
            eventProperties.SetProperty("stringProp", $"stringPropValue{seed}");
            eventProperties.SetProperty("stringMultilineProp", $"line1_{seed}\r\nline2\r\nline3");
            eventProperties.SetProperty("longProp", 123L + seed);
            eventProperties.SetProperty("doubleProp", (double)123.123 + seed);
            eventProperties.SetProperty("dateTimeProp", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed));
            eventProperties.SetProperty("boolProp", true);
            eventProperties.SetProperty("guidProp", new Guid(seed, 123, 456, 1, 2, 3, 4, 5, 6, 7, 8));
            eventProperties.SetProperty("stringPropPii", $"stringPropValue{seed}", Applications.Events.PiiKind.GenericData);
            eventProperties.SetProperty("longPropPii", 123L + seed, Applications.Events.PiiKind.GenericData);
            eventProperties.SetProperty("doublePropPii", (double)123.123 + seed, Applications.Events.PiiKind.GenericData);
            eventProperties.SetProperty("dateTimePropPii", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed), Applications.Events.PiiKind.GenericData);
            eventProperties.SetProperty("boolPropPii", true, Applications.Events.PiiKind.GenericData);
            eventProperties.SetProperty("guidPropPii", new Guid(seed, 890, 456, 1, 2, 3, 4, 5, 6, 7, 8), Applications.Events.PiiKind.GenericData);
            return eventProperties;
        }

        internal static LogEventCommand CreateLogEventCommand(int seed) =>
            new LogEventCommand(CreateEventProperties(seed));

        internal static IEnumerable<LogEventCommand> CreateLogEventCommands()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return CreateLogEventCommand(seed: i);
            }
        }

        internal static IEnumerable<SetContextCommand> CreateSetContextCommands()
        {
            var setContextArgs = new SetContextArgs[]
            {
                new SetContextArgs("commonString", "commonStringValue", TelemetryPropertyType.String, false),
                new SetContextArgs("commonBool", true, TelemetryPropertyType.Boolean, false),
                new SetContextArgs("commonDateTime", new DateTime(2021, 08, 12, 08, 09, 10), TelemetryPropertyType.DateTime, false),
                new SetContextArgs("commonDouble", (double)123.123, TelemetryPropertyType.Double, false),
                new SetContextArgs("commonGuid", Guid.Parse("ab39e8a2-cd8d-4a1e-8b60-b8b320066f5d"), TelemetryPropertyType.Guid, false),
                new SetContextArgs("commonLong", 123L, TelemetryPropertyType.Long, false),
                new SetContextArgs("commonStringPii", "commonStringValue", TelemetryPropertyType.String, true),
                new SetContextArgs("commonBoolPii", true, TelemetryPropertyType.Boolean, true),
                new SetContextArgs("commonDateTimePii", new DateTime(2021, 08, 12, 08, 09, 10), TelemetryPropertyType.DateTime, true),
                new SetContextArgs("commonDoublePii", (double)123.123, TelemetryPropertyType.Double, true),
                new SetContextArgs("commonGuidPii", Guid.Parse("a96106d9-ed92-42e3-87bc-8d3a1d10e120"), TelemetryPropertyType.Guid, true),
                new SetContextArgs("commonLongPii", 123L, TelemetryPropertyType.Long, true),
            };
            return setContextArgs.Select((args) => new SetContextCommand(args));
        }

        internal static string GetOutOfProcessExecutablePath()
        {
            var binPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
            var exePath = Path.Combine(binPath, "outofprocess.exe");
            var dllPath = Path.Combine(binPath, "outofprocess.dll");
            var linuxPath = Path.Combine(binPath, "outofprocess");

            if (File.Exists(exePath) && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return exePath;
            }

            if (File.Exists(linuxPath)
                && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)))
            {
                return linuxPath;
            }

            if (File.Exists(dllPath))
            {
                return dllPath;
            }

            throw new FileNotFoundException("Couldn't find outofprocess.dll or outofprocess.exe");
        }
    }
}
