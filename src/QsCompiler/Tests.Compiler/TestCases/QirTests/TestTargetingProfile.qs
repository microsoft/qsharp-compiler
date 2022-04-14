// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    //function TakesTuple()

    function SumArray(arr : Int[]) : Int {
        mutable sum = 0;
        for item in arr{
            set sum += item;
        }
        return sum;
    }

    @EntryPoint()
    operation TestProfileTargeting() : Unit {
        let sum = SumArray([1,2,3]);
    }
}
