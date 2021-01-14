// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestArrayUpdate(y : String[], b : String) : String[]
    {
        mutable x = y;
        set x w/= 0 <- b;
        set x w/= 1 <- "Hello";
        return x;
    }
}
