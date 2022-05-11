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
    
    //function SumArray(arr : Int[]) : Int {
    //    mutable sum = 0;
    //    for item in arr{
    //        set sum += item;
    //    }
    //    return sum; // Not permitted (mutable to immutable assignment)
    //}

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
    operation TestProfileTargeting() : Int {
        let arr1 = [1,2,3];
        DumpMachine(arr1);
        
        //let sum = SumArray(arr1);
        mutable sum = 0;
        for item in arr1 {
            set sum += item;
        }

        let arr2 = [6, size = 3];
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
        
        let tupleArr = [(2, 0.), (1, 1.), (3, 2.)];
        let (pauli, _) = tupleArr[1];
        DumpMachine(pauli);
        
        let arrTuple = ([1,2], true);
        let (vals, _) = arrTuple;
        DumpMachine(vals[1]);
        DumpMachine(vals);
        
        let arrArr = [[2, 1], [], [3], [0]];
        let pauliY = arrArr[2][0];
        DumpMachine(pauliY);
        DumpMachine((arrArr[1], arrArr[3]));
        DumpMachine(arrArr);
        
        let updatedArrArr1 = arrArr w/ 0 <- [];
        let updatedArrArr2 = arrArr w/ 1 <- [1,2,3];
        DumpMachine(updatedArrArr1);
        DumpMachine(updatedArrArr2);

        //let tupleArr = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        //let (pauli, _) = tupleArr[1];
        //LogPauli(pauli);

        //let arrArr = [[PauliX, PauliZ], [PauliY], [PauliI]];
        //let pauliI = arrArr[2][0];
        //LogPauli(pauliI);

        return sum; // m1 == m2 ? sum | 0; // TODO: check branching
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
