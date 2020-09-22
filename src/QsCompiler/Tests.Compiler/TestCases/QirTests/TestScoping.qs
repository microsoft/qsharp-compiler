// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestScoping (a : Int[]) : Int
    {
        mutable sum = 0;

        for (i in a)
        {
            if (i > 5)
            {
                let x = i + 3;
                set sum = sum + x;
            }
            else
            {
                let x = i * 2;
                set sum = sum + x;
            }
        }
        for (i in a)
        {
            set sum = sum + i;
        }

        return sum;
    }
}