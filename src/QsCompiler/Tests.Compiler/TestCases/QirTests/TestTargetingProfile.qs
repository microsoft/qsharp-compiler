// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Diagnostics;

    //function TakesTuple()

    function SumArray(arr : Int[]) : Int {
        mutable sum = 0;
        for item in arr{
            set sum += item;
        }
        return sum;
    }

    @EntryPoint()
    operation TestProfileTargeting() : Result {
        let arr1 = [1,2,3];
        DumpMachine(arr1);

        let sum = SumArray(arr1);
        let arr2 = [sum, size = 3];
        DumpMachine(arr2);

        return Zero;
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
