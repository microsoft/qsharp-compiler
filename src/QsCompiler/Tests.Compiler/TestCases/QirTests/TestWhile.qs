// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestWhile (a : Int, b : Int) : Int
    {
        mutable n = a;
        while (n < b)
        {
            set n = n * 2;
        }
        return n;
    }
}
