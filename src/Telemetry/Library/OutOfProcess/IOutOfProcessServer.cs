// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal interface IOutOfProcessServer
    {
        void ProcessCommand(OutOfProcessQuitCommand command);

        void ProcessCommand(OutOfProcessLogEventCommand command);

        void ProcessCommand(OutOfProcessSetContextCommand command);
    }
}