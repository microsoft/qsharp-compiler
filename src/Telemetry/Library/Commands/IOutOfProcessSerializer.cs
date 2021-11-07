// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Quantum.Telemetry.Commands
{
    public interface ICommandSerializer
    {
        IEnumerable<string> Write(IEnumerable<CommandBase> commands);

        IEnumerable<string> Write(CommandBase command);

        IEnumerable<CommandBase> Read(IEnumerable<string> messages);
    }
}
