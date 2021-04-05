// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestUnreachable (a : Int, b : Int) : Int
    {
        let c = a + b;

        if (c == 5)
        {
            return c;
            let d = 2 + a;
            fail "not reachable";
            return b;
            let e = 3 + b;
            return e;
            let f = c + e;
        }
        else
        {
            return b;
            return c;
        }

        let f = c + b;
        return f;
        return c;
    }
}
