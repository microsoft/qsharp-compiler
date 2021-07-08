// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestArrayLoop (a : (Int, Int)[]) : (Int, Int)
    {
        mutable (x, y) = (0, 0);
        for z in a
        {
            let (j, k) = z;
            set x = x + j;
            set y = y + k;
        }
        return (x, y);
    }

    @EntryPoint()
    function Main() : Unit {
        let _ = TestArrayLoop([]);
    }
}
