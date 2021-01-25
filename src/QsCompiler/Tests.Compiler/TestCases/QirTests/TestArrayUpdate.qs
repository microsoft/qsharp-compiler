// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype Complex = (Re : Double, Im : Double);

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

    function TestArrayUpdate4(array : String[]) : String[] 
    {
        let item = "Hello";
        mutable arr = new String[0];
        set arr = array;

        for (i in 0 .. 9)
        {
            set arr w/= i <- item; 
        }

        return arr;
    }

    function TestArrayUpdate5(cond : Bool, array : Complex[]) : Complex[]
    {
        let item = Complex(0.,0.);
        mutable arr = array;
        for (i in 0 .. 2 .. Length(arr) - 1)
        {
            set arr w/= i <- arr[i % 2]; 
            if (cond)
            {
                set arr w/= i % 2 <- item; 
            }
        }

        return arr;
    }
}
