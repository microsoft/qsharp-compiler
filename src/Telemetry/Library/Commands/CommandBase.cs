// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.Commands
{
    public abstract class CommandBase
    {
        public CommandBase(CommandType commandType, object? args = null)
        {
            this.CommandType = commandType;
            this.Args = args;
        }

        public CommandType CommandType { get; set; }

        public object? Args { get; set; }

        public abstract void Process(ICommandProcessor server);
    }
}
