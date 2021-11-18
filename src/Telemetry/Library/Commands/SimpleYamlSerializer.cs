// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry.Commands
{
    /// <summary>
    /// This is a simplified Yaml serializer to serialize and deserialize specific
    /// objects (of CommandBase descendent types) for two purposes:
    /// 1. Send telemetry commands to an out-of-process telemetry uploader
    ///    (see OutOfProcessLogger and OutOfProcessServer)
    /// 2. Print events to the console for debugging purposes
    ///
    /// Supported types to be Serialized and Deserialized:
    /// - SetContextCommand
    /// - QuitCommand
    /// - LogEventCommand
    ///
    /// Serialization format:
    ///   The serialization format is a simplified version of Yaml 1.1 (https://yaml.org/spec/1.1/)
    ///   in which we serialize/deserialize a list of telemetry commands, specifying its type
    ///   and each command can contain a list of properties, also with their types specified.
    ///   Everything else is ignored.
    ///
    /// Example of a LogEvent command:
    ///
    /// ```yaml
    /// - command: !LogEvent
    ///     __name__: !String eventName0
    ///     stringProp: !String stringPropValue0
    ///     stringMultilineProp: !String line1_0__\r\n__line2__\r\n__line3
    ///     stringPropPii: !String+Pii stringPropValue0
    /// ```
    ///
    /// Please see the SimpleYamlSerializerTests for more examples.
    ///
    /// Notes:
    /// - As with the Yaml spec, the `-` prefixes an item in a list
    /// - As with the Yaml spec, the `:` is a separator between a key and a value
    /// - As with the Yaml spec, the `!` prefixes the user-defined type of the value
    /// - The user-defined types that we use are one of the values of the CommandType enum for command items,
    ///   or the TelemetryPropertyType enum for property items
    /// - As with the Yaml spec, indentation is used to determine sub-nodes (properties in our case)
    ///   but different than Yaml, we only support two levels of indentation:
    ///   - The zero indentation for the commands
    ///   - The four-space indentation for properties
    /// - Different than Yaml, we escape all new line characters from a string property value with a
    ///   `__\r\n__` constant.
    /// - Specifically for the LogEvent command, we serialize the event name in the `__name__` property.
    /// - Different than Yaml, everything else that does not match the above formats will be ignored.
    ///
    /// </summary>
    public class SimpleYamlSerializer : ICommandSerializer
    {
        private static readonly string LineBreak = @"__\r\n__";
        private static readonly string EventNamePropertyName = "__name__";
        private static readonly Regex CommandRegex = new Regex(@"^((?<command>- command:\s+!(?<commandType>[^\s$]+).*$)|(?<property>\s{4}(?<key>[^\s:]+):\s*(!(?<type>[^\s:\+]+)(?<pii>\+Pii)?)?\s*(?<value>.+)?))$", RegexOptions.Compiled);
        private static readonly Regex EscapeLineBreaksRegex = new Regex(@"(\r\n|\n\r|\n|\r)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex ReplaceLineBreaksRegex = new Regex(LineBreak.Replace(@"\", @"\\"), RegexOptions.Compiled);

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

            if (value is DateTime || value is DateTime?)
            {
                return $"{value:O}";
            }

            return $"{value}";
        }

        private static string ReplaceLineBreaks(string text) =>
            ReplaceLineBreaksRegex.Replace(text, "\r\n");

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

        public IEnumerable<string> Write(IEnumerable<CommandBase> commands) =>
            commands.SelectMany((command) => this.Write(command));

        public IEnumerable<string> Write(CommandBase command)
        {
            yield return $"- command: !{command.CommandType}";

            if (command is SetContextCommand setContextCommand)
            {
                yield return SetContextArgsToYamlString(setContextCommand.Args);
            }
            else if (command is LogEventCommand logEventCommand)
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

        public IEnumerable<CommandBase> Read(IEnumerable<string> messages)
        {
            var enumerator = messages.GetEnumerator();
            CommandBase? command = null;

            while (enumerator.MoveNext())
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
                            case nameof(CommandType.LogEvent):
                                command = new LogEventCommand();
                                break;
                            case nameof(CommandType.Quit):
                                yield return new QuitCommand();
                                break;
                            case nameof(CommandType.SetContext):
                                command = new SetContextCommand();
                                break;
                            default:
                                if (TelemetryManagerConstants.IsDebugBuild || TelemetryManager.TestMode)
                                {
                                    TelemetryManager.LogToDebug($"Unexpected YAML commandType: {commandType}");
                                }

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

                        if (command is LogEventCommand logEventCommand)
                        {
                            SetLogEventProperty(logEventCommand, key, type, value, piiKind);
                        }
                        else if (command is SetContextCommand setContextCommand)
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
                    if (TelemetryManagerConstants.IsDebugBuild || TelemetryManager.TestMode)
                    {
                        TelemetryManager.LogToDebug($"Unexpected YAML string: {line}");
                    }
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

        private static void SetLogEventProperty(LogEventCommand logEventCommand, string key, string type, string value, PiiKind piiKind)
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
                    if (TelemetryManagerConstants.IsDebugBuild || TelemetryManager.TestMode)
                    {
                        TelemetryManager.LogToDebug($"Unexpected YAML type: {type}");
                    }

                    break;
            }
        }
    }
}
