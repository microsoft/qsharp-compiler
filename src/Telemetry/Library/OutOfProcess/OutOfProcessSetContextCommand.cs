// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessSetContextCommand : OutOfProcessCommand
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