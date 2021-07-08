// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestResults (a : Result, b : Result) : Result
    {
        if (a == b)
        {
            return One;
        }
        elif (a == One)
        {
            return b;
        }
        return Zero;
    }
}
