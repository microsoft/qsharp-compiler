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

    operation CheckLength (bases : Pauli[], qubits : Qubit[]) : Result {
        if Length(bases) != Length(qubits) {
            DumpMachine(bases);
        }
        return One;
    }

    function CheckInlining1 (b : Bool) : Bool {

        let a = Zero;
        let c = a == One or b;
        return c;
    }

    operation Slicing() : Unit
    {
        use q = Qubit[5];
        let r1 = M(q[1]);
        let r2 = M(q[4]);

        let r3 = M(q[0..-1..4][1]);
        let z1 = q[0..-1..4];
        let r4 = M(z1[4]);

        let r5 = M(q[4..-1..0][1]);
        let z2 = q[4..-1..0];
        let r6 = M(z2[4]);
    }

    @EntryPoint()
    operation TestProfileTargeting() : (Int, Int) {
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
        let (intValue, _) = tupleArr[1];
        DumpMachine(intValue);

        let tupleArr2 = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        let (pauli, _) = tupleArr2[1];
        LogPauli(pauli);

        let arrTuple = ([1,2], true);
        let (vals, _) = arrTuple;
        DumpMachine(vals[1]);
        DumpMachine(vals);

        let arrArr = [[2, 1], [], [3], [0]];
        let pauliY = arrArr[2][0];
        DumpMachine(pauliY);
        DumpMachine((arrArr[1], arrArr[3]));
        DumpMachine(arrArr);

        let arrArr2 = [[PauliX, PauliZ], [PauliY], [PauliI]];
        let pauliI = arrArr2[2][0];
        LogPauli(pauliI);

        let updatedArrArr1 = arrArr w/ 0 <- [];
        let updatedArrArr2 = arrArr w/ 1 <- [1,2,3];
        let updatedArrArr3 = arrArr2 w/ 0 <- [];
        let updatedArrArr4 = arrArr2 w/ 1 <- [PauliX, PauliX, PauliX];
        DumpMachine(updatedArrArr1);
        DumpMachine(updatedArrArr2);
        DumpMachine(updatedArrArr3);
        DumpMachine(updatedArrArr4);

        use (qs1, qs2, q) = (Qubit[2], Qubit[1], Qubit());
        let qubitArrArr = [qs1, [], qs2, [q]];
        DumpMachine(qubitArrArr w/ 0 <- []);
        DumpMachine(qubitArrArr w/ 1 <- [q, q, q]);

        TestIfClauses1(m1);
        TestIfClauses2();
        TestIfClauses3();
        TestIfClauses4(m2);

        mutable rand = 0;
        for item in qs {
            set rand <<<= 1;
            if CheckLength([PauliX], [item]) == One {
                set rand += 1;
            }
        }

        let a = m1 == Zero;
        let b = CheckInlining1(a);
        DumpMachine((a, b));

        mutable foo = m1 == m2 ? sum | 0;
        if (m1 == Zero)
        {
            mutable bar = 0;
            for anc in qs {
                set bar += M(anc) == One ? 1 | 0;
            }
            set foo = bar;
        }
        else
        {
            mutable bar = 0;
            for anc in qs {
                set bar += M(anc) == Zero ? 1 | 0;
            }
            set foo = bar;
        }

        Slicing();

        let arr3 = [m1, size = 1];
        if (m2 != m1)
        {
            DumpMachine(arr3 w/ 0 <- m2);
        }

        return (sum, rand);
    }


    operation TestIfClauses1(m : Result) : Unit {

        if (false) {
            use q = Qubit();
            DumpMachine((1, M(q)));
        }
        elif (m == Zero) {
            use q = Qubit();
            DumpMachine((2, M(q)));
        }
        elif (false) {
            use q = Qubit();
            DumpMachine((3, M(q)));
        }
        else {
            use q = Qubit();
            DumpMachine((4, M(q)));
        }
    }

    operation TestIfClauses2() : Unit {

        if (false)
        {
            use q = Qubit();
            DumpMachine((5, M(q)));
        }
        elif (true)
        {
            use q = Qubit();
            DumpMachine((6, M(q)));
        }
        elif (true)
        {
            use q = Qubit();
            DumpMachine((7, M(q)));
        }
        else
        {
            use q = Qubit();
            DumpMachine((8, M(q)));
        }
    }

    operation TestIfClauses3() : Unit {

        use q2 = Qubit();
        if (true)
        {
            use q = Qubit();
            DumpMachine((9, M(q)));
        }
        elif (M(q2) == Zero)
        {
            use q = Qubit();
            DumpMachine((10, M(q)));
        }
        else
        {
            use q = Qubit();
            DumpMachine((11, M(q)));
        }
    }

    operation TestIfClauses4(m : Result) : Unit {

        if (m == Zero)
        {
            use q = Qubit();
            DumpMachine((12, M(q)));
        }
        else
        {
            use q = Qubit();
            DumpMachine((13, M(q)));
        }
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
