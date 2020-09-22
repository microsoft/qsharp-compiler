// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestArrayUpdate(y : Int[], a : Int, b : Int) : Int[]
    {
        mutable x = y;
        set x w/= a <- b;
        return x;
    }
}
