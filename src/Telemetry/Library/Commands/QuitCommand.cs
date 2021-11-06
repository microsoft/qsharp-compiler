// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.Commands
{
    public class QuitCommand : CommandBase
    {
        public QuitCommand()
            : base(CommandType.Quit)
        {
        }

        public override bool Equals(object? obj) =>
             obj is QuitCommand;

        public override int GetHashCode() =>
            this.CommandType.GetHashCode();

        public override void Process(ICommandProcessor server) =>
            server.ProcessCommand(this);
     }
}
