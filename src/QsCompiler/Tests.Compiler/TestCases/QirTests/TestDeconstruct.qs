// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestDeconstruct (a : (Int, (Int, Int))) : Int
    {
        let (x, y) = a;
        mutable b = 3;
        mutable c = 5;
        set (b, c) = y;
        return x + b * c;
    }

    @EntryPoint()
    function Main() : Unit {
        let _ = TestDeconstruct(0, (0, 0));
    }
}
