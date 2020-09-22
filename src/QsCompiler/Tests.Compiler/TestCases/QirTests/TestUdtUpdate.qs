// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Double, A : Int), B : Int);

    function TestUdtUpdate(a : Int, b : Int) : TestType
    {
        mutable x = TestType((1.0, a), b);
        set x w/= A <- 2;
        return x;
    }
}
