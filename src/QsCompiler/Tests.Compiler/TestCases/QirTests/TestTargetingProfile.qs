// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Diagnostics;

    newtype MyUnit = Unit;
    newtype MyTuple = (Item1 : Int, Item2 : Double);
    newtype MyNestedTuple = ((Item1 : Int, Item2 : Double), Item3 : Double);

    //function TakesTuple()

    function LogPauli(pauli : Pauli) : Unit {
        body intrinsic;
    }

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
    operation TestProfileTargeting(cond : Bool) : Result {
        let arr1 = [1,2,3];
        DumpMachine(arr1);

        let sum = SumArray(arr1);
        let arr2 = [sum, size = 3];
        DumpMachine(arr2);

        for i in 0 .. Length(arr1)-1 {
            DumpMachine(arr1[i]);
        }

        let concatenated = arr1 + arr2;
        DumpMachine(concatenated);
        //let slice = concatenated[arr1[0]..arr1[1]...]; // bug in type inference
        let slice1 = concatenated[arr2[2]-1..-arr1[1]..arr1[0]];
        let slice2 = concatenated[arr1[0]..arr1[2]];
        DumpMachine(slice1);
        DumpMachine(slice2);

        let updated = arr1 w/ 0 <- 4;
        DumpMachine(updated);

        let udt1 = MyTuple(1, 1.);
        DumpMachine((udt1, udt1 w/ Item1 <- 5));
        DumpMachine(udt1 w/ Item2 <- 2.);
        DumpMachine(MyUnit());

        let udt2 = MyNestedTuple(udt1!, 0.);
        let udt2update = udt2 w/ Item2 <- 3.;
        DumpMachine(udt2);
        DumpMachine(udt2update);
        DumpMachine(udt2update::Item2);

        use qs = Qubit[2];
        H(qs[0]);
        CNOT(qs[0], qs[1]);
        let (m1, m2) = (M(qs[0]), M(qs[1]));

        let tupleArr = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        let (pauli, _) = tupleArr[1];
        LogPauli(pauli);

        //let arrTuple = ([1,2], true); // FIXME: CORRECT TYPE NOT IMPLEMENTED
        //let (vals, _) = arrTuple;
        //DumpMachine(vals[1]);

        //let arrArr = [[PauliX, PauliZ], [PauliY], [PauliI]];
        //let pauliI = arrArr[2][0];
        //LogPauli(pauliI);

        // let (a, b) = (1,1);
        // mutable item = arr1[a + b];
        // set item = arr1[sum - 5];
        // 
        // mutable idx = 0;
        // if cond {
        //     set idx = 2;
        //     DumpMachine(arr1[idx]);
        // }
        // //set item = arr1[idx]; // doesn't work, since the cache is outdated and the value is reloaded
        // 
        // DumpMachine(arr1[arr1[a]]);
        // let arr3 = [idx, 5, item];
        // //DumpMachine(arr3[arr3[0]]); // doesn't work (cond cannot be evaluated, hence the value of idx is unknown)
        // 
        // let arr4 = [a, 5, item];
        // DumpMachine(arr4[arr4[0]]);

        return m1; // m1 == m2 ? Zero | One; FIXME: results in a "the target Unspecified does not support comparing measurement results"
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
