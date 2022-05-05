namespace Microsoft.Quantum.Qir.Development {
    open Microsoft.Quantum.Diagnostics;

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
    operation RunExample() : Result {
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

        use qs = Qubit[2];
        H(qs[0]);
        CNOT(qs[0], qs[1]);
        let (m1, m2) = (M(qs[0]), M(qs[1]));

        let tupleArr = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        let (pauli, _) = tupleArr[1];
        LogPauli(pauli);

        return m1; // m1 == m2 ? Zero | One; FIXME: results in a "the target Unspecified does not support comparing measurement results"
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Core{

    @Attribute()
    newtype Attribute = Unit;

    @Attribute()
    newtype Inline = Unit;

    @Attribute()
    newtype EntryPoint = Unit;

    function Length<'T> (array : 'T[]) : Int { body intrinsic; }

    function RangeStart (range : Range) : Int { body intrinsic; }

    function RangeStep (range : Range) : Int { body intrinsic; }

    function RangeEnd (range : Range) : Int { body intrinsic; }

    function RangeReverse (range : Range) : Range { body intrinsic; }
}

namespace Microsoft.Quantum.Targeting {

    @Attribute()
    newtype TargetInstruction = String;
}

