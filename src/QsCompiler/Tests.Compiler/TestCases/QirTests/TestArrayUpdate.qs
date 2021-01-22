// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestArrayUpdate1(even : String) : String[]
    {
        mutable arr = new String[10];
        for (i in 0 .. 9)
        {
            let str = i % 2 != 0 ? "odd" | even;
            set arr w/= i <- str; 
        }

        return arr;
    }

    function TestArrayUpdate2(array : String[], even : String) : String[]
    {
        mutable arr = array;
        for (i in 0 .. 9)
        {
            let str = i % 2 != 0 ? "odd" | even;
            set arr w/= i <- str; 
        }

        return arr;
    }

    function TestArrayUpdate3(y : String[], b : String) : String[]
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
