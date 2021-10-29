// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Quantum.Telemetry.OutOfProcess
{
    internal interface IOutOfProcessSerializer
    {
        IEnumerable<string> Write(IEnumerable<OutOfProcessCommand> commands);

        IEnumerable<string> Write(OutOfProcessCommand command);

        IAsyncEnumerable<OutOfProcessCommand> Read(IAsyncEnumerable<string> messages);
    }
}