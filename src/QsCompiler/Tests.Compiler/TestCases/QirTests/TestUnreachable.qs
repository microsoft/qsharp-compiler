// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestUnreachable1 (a : Int, b : Int) : Int
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

    function TestUnreachable2 (a : Int, b : Int) : Int
    {
        let c = a + b;
        return c;

        if (c == 5)
        {
            return a;
        }
        else
        {
            return b;
        }
    }

    function TestUnreachable3 (a : Int) : Int
    {
        mutable results = new Int[0];
        return a;
        
        for index in 0 .. a - 1 {
            set results += [index];
        }

        return a;
    }
}
