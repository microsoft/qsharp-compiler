// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal class OutOfProcessQuitCommand : OutOfProcessCommand
    {
        public OutOfProcessQuitCommand()
            : base(OutOfProcessCommandType.Quit)
        {
        }

        public override bool Equals(object? obj) =>
             obj is OutOfProcessQuitCommand;

        public override int GetHashCode() =>
            this.CommandType.GetHashCode();

        public override void Process(IOutOfProcessServer server) =>
            server.ProcessCommand(this);
     }
}