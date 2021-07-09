// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestRange () : Range
    {
        let x = 0..2..6;
        let a = [0, 2, 4, 6, 8, 10, 12, 14, 16];
        let b = a[x];

        let y = 0..4;
        for j in y
        {
            let m = 1;
        }
        return x;
    }
}
