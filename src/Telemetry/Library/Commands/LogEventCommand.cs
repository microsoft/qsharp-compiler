// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry.Commands
{
    public class LogEventCommand : CommandBase
    {
        public LogEventCommand(EventProperties? eventProperties = null)
            : base(CommandType.LogEvent, eventProperties ?? new EventProperties())
        {
        }

        public new EventProperties Args
        {
            get => (EventProperties)base.Args!;
        }

        public override bool Equals(object? obj)
        {
            if (obj is LogEventCommand logEventCommand)
            {
                if ((logEventCommand.Args.Name != this.Args.Name)
                    || (logEventCommand.Args.PiiProperties.Count != this.Args.PiiProperties.Count)
                    || (logEventCommand.Args.Properties.Count != this.Args.Properties.Count))
                {
                    return false;
                }

                foreach (var property in this.Args.Properties)
                {
                    var isPii = this.Args.PiiProperties.ContainsKey(property.Key);
                    if (!logEventCommand.Args.Properties.TryGetValue(property.Key, out var value)
                        || !Equals(value, property.Value)
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
            this.CommandType.GetHashCode() ^ this.Args.Name.GetHashCode();

        public override void Process(ICommandProcessor server) =>
            server.ProcessCommand(this);
    }
}
