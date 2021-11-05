// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.Commands
{
    internal class SetContextCommand : CommandBase
    {
        public SetContextCommand(SetContextArgs? setContextArgs = null)
            : base(CommandType.SetContext, setContextArgs ?? new())
        {
        }

        public new SetContextArgs Args
        {
            get => (SetContextArgs)base.Args!;
        }

        public override bool Equals(object? obj) =>
            (obj is SetContextCommand setContextCommand)
            && object.Equals(this.Args, setContextCommand.Args);

        public override int GetHashCode() =>
            this.CommandType.GetHashCode() ^ this.Args.GetHashCode();

        public override void Process(ICommandProcessor server) =>
            server.ProcessCommand(this);
    }

    internal record SetContextArgs
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
    }
}
