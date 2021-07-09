// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestBools (a : Bool, b : Bool) : Bool
    {
        let c = a == b ? a | b;
        let d = a and b;
        let e = a or b;
        let f = not a;
        if (f)
        {
            return d;
        }
        else
        {
            return e;
        }
    }
}
