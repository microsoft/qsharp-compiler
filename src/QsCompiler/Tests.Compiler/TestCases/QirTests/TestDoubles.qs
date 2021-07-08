// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestDouble (x : Double, y : Double) : Double
    {
        let a = x + y - 2.0;
        let b = a * 1.235 + x ^ y;
        let c = a >= b ? a - b | a + b;
        return a * b * c;
    }
}
