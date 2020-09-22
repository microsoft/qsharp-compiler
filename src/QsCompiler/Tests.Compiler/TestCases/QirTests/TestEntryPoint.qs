// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    //open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation TestEntryPoint(a : Double[], b : Bool) : Double
    {
        mutable sum = 0.0;
        mutable flag = b;
        for (i in a)
        {
            set sum = sum + (flag ? i | -i);
            set flag = not flag;
        }
        return sum;
    }
}
