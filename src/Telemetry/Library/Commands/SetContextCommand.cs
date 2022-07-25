// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.Commands
{
    public class SetContextCommand : CommandBase
    {
        public SetContextCommand(SetContextArgs? setContextArgs = null)
            : base(CommandType.SetContext, setContextArgs ?? new SetContextArgs())
        {
        }

        public new SetContextArgs Args
        {
            get => (SetContextArgs)base.Args!;
        }

        public override bool Equals(object? obj) =>
            (obj is SetContextCommand setContextCommand)
            && Equals(this.Args, setContextCommand.Args);

        public override int GetHashCode() =>
            this.CommandType.GetHashCode() ^ this.Args.GetHashCode();

        public override void Process(ICommandProcessor server) =>
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

        public override bool Equals(object? obj)
        {
            if (obj is SetContextArgs setContextArgs)
            {
                return Equals(setContextArgs.IsPii, this.IsPii)
                       && Equals(setContextArgs.Name, this.Name)
                       && Equals(setContextArgs.PropertyType, this.PropertyType)
                       && Equals(setContextArgs.Value, this.Value);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (this.Name?.GetHashCode() ?? 0) ^ this.PropertyType.GetHashCode();
        }
    }
}
