// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Quantum.Telemetry.OutOfProcess;

namespace Microsoft.Quantum.Telemetry.Tests.OutOfProcess
{
    public class OutOfProcessCommandsTestCommon
    {
        internal class NullOutOfProcessServer : IOutOfProcessServer
        {
            public OutOfProcessCommand? LastProcessedCommand { get; private set; }

            public Type? LastProcessedType { get; private set; }

            private void InternalProcess<T>(T command)
                where T : OutOfProcessCommand
            {
                this.LastProcessedCommand = command;
                this.LastProcessedType = typeof(T);
            }

            public void ProcessCommand(OutOfProcessQuitCommand command) =>
                this.InternalProcess(command);

            public void ProcessCommand(OutOfProcessLogEventCommand command) =>
                this.InternalProcess(command);

            public void ProcessCommand(OutOfProcessSetContextCommand command) =>
                this.InternalProcess(command);
        }

        internal static NullOutOfProcessServer CreateNullOutOfProcessServer() => new();

        internal static OutOfProcessQuitCommand CreateOutOfProcessQuitCommand() => new();

        internal static OutOfProcessLogEventCommand CreateOutOfProcessLogEventCommand(int seed)
        {
            OutOfProcessLogEventCommand outOfProcessLogEventCommand = new(new Applications.Events.EventProperties());
            outOfProcessLogEventCommand.Args.Name = $"eventName{seed}";
            outOfProcessLogEventCommand.Args.SetProperty("stringProp", $"stringPropValue{seed}");
            outOfProcessLogEventCommand.Args.SetProperty("stringMultilineProp", $"line1_{seed}\r\nline2\r\nline3");
            outOfProcessLogEventCommand.Args.SetProperty("longProp", 123L + seed);
            outOfProcessLogEventCommand.Args.SetProperty("doubleProp", (double)123.123 + seed);
            outOfProcessLogEventCommand.Args.SetProperty("dateTimeProp", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed));
            outOfProcessLogEventCommand.Args.SetProperty("boolProp", true);
            outOfProcessLogEventCommand.Args.SetProperty("guidProp", new Guid(seed, 123, 456, 1, 2, 3, 4, 5, 6, 7, 8));
            outOfProcessLogEventCommand.Args.SetProperty("stringPropPii", "stringPropValue{seed}", Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("longPropPii", 123L + seed, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("doublePropPii", (double)123.123 + seed, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("dateTimePropPii", new DateTime(2021, 08, 12, 08, 09, 10) + TimeSpan.FromHours(seed), Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("boolPropPii", true, Applications.Events.PiiKind.GenericData);
            outOfProcessLogEventCommand.Args.SetProperty("guidPropPii", new Guid(seed, 890, 456, 1, 2, 3, 4, 5, 6, 7, 8), Applications.Events.PiiKind.GenericData);
            return outOfProcessLogEventCommand;
        }

        internal static IEnumerable<OutOfProcessLogEventCommand> CreateOutOfProcessLogEventCommands()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return CreateOutOfProcessLogEventCommand(seed: i);
            }
        }

        internal static IEnumerable<OutOfProcessSetContextCommand> CreateOutOfProcessSetContextCommands()
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
            return setContextArgs.Select((args) => new OutOfProcessSetContextCommand(args));
        }
    }
}
