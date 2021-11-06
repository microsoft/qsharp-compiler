// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.Commands
{
    public interface ICommandProcessor
    {
        void ProcessCommand(QuitCommand command);

        void ProcessCommand(LogEventCommand command);

        void ProcessCommand(SetContextCommand command);
    }
}
