// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class SimpleYamlSerializer : IOutOfProcessSerializer
    {
        private static readonly string LineBreak = @"__\r\n__";
        private static readonly string EventNamePropertyName = "__name__";
        private static readonly Regex CommandRegex = new(@"^((?<command>- command:\s+!(?<commandType>[^\s$]+).*$)|(?<property>\s{4}(?<key>[^\s:]+):\s*(!(?<type>[^\s:\+]+)(?<pii>\+Pii)?\s+)?(?<value>.+)))$", RegexOptions.Compiled);
        private static readonly Regex EscapeLineBreaksRegex = new(@"(\r\n|\n\r|\n|\r)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ReplaceLineBreaksRegex = new(LineBreak.Replace(@"\", @"\\"), RegexOptions.Compiled);

        private static string PiiType(bool isPii) =>
            isPii ? "+Pii" : "";

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
}