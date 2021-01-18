// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestArrayUpdate(y : String[], b : String) : String[]
    {
        mutable x = y;
        set x w/= 0 <- b;
        set x w/= 1 <- "Hello";

        mutable arr = new (Int, Int)[10];
        for (i in 0 .. 9)
        {
            set arr w/= i <- (i, i + 1); 
        }

        return x;
    }
}
