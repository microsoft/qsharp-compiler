// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal abstract class OutOfProcessCommand
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
}