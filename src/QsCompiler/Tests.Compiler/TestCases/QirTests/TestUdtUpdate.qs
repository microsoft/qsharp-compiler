// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Double, A : String), B : Int);

    function TestUdtUpdate(a : String, b : Int) : TestType
    {
        mutable x = TestType((1.0, a), b);
        set x w/= A <- "Hello";
        return x;
    }
}
