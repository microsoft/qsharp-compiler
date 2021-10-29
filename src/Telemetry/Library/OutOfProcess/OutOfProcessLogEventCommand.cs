// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Applications.Events;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessLogEventCommand : OutOfProcessCommand
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
}