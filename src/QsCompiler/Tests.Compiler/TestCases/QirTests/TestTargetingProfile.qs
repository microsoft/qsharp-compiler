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

    operation CNOT(control: Qubit, target: Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }

    operation H(qubit : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }

    operation M(qubit : Qubit) : Result {
        body intrinsic;
    }

    @EntryPoint()
    operation TestProfileTargeting() : Result {
        let arr1 = [1,2,3];
        DumpMachine(arr1);

        let sum = SumArray(arr1);
        let arr2 = [sum, size = 3];
        DumpMachine(arr2);

        use qs = Qubit[2];
        H(qs[0]);
        CNOT(qs[0], qs[1]);

        let (m1, m2) = (M(qs[0]), M(qs[1]));
        return m1; // m1 == m2 ? Zero | One; FIXME: results in a "the target Unspecified does not support comparing measurement results"
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
