namespace Microsoft.Quantum.Testing.ExecutionTests {

    open Microsoft.Quantum.Intrinsic;

    newtype MyUnit = Unit;
    newtype MyTuple = (Item1 : Int, Item2 : Double);
    newtype MyNestedTuple = ((Item1 : Int, Item2 : Double), Item3 : Double);

    function SumArray(arr : Int[]) : Int {
        mutable sum = 0;
        for item in arr{
            set sum += item;
        }
        return sum;
    }

    @EntryPoint()
    operation TestNativeTypeHandling() : Result {
        let arr1 = [1,2,3];
        Message($"{arr1}");

        let sum = SumArray(arr1);
        let arr2 = [sum, size = 3];
        Message($"{arr2}");

        for i in 0 .. Length(arr1)-1 {
            Message($"item {i} is {arr1[i]}");
        }

        let concatenated = arr1 + arr2;
        Message($"{concatenated}");
        let slice1 = concatenated[arr2[2]-1..-arr1[1]..arr1[0]];
        let slice2 = concatenated[arr1[0]..arr1[2]];
        Message($"{slice1}, {slice2}");

        let updated = arr1 w/ 0 <- 4;
        Message($"{arr1}, {updated}");

        let udt1 = MyTuple(1, 1.);
        let udt2 = MyNestedTuple(udt1!, 0.);
        Message($"{MyUnit()}");
        Message($"{udt1 w/ Item1 <- 5}, {udt1 w/ Item2 <- 2.}, {udt1}");
        Message($"{udt2}");
        Message($"{udt2 w/ Item2 <- 3.}");

        let tupleArr = [(2, 0.), (1, 1.), (3, 2.)];
        let (pauli, _) = tupleArr[1];
        Message($"{pauli}");

        let arrTuple = ([1,2], true);
        let (vals, _) = arrTuple;
        Message($"{vals[1]}, {vals}");

        let arrArr = [[2, 1], [], [3], [0]];
        let pauliY = arrArr[2][0];
        Message($"{arrArr}");
        Message($"{arrArr[1]}, {arrArr[3]}, {pauliY}");

        // TODO: write tests for array of array of array

        // TODO: we need to change how paulis work here, too.
        // The load of the global constant makes it that the constant folding
        // doesn't recognize the array length as constant.

        //let tupleArr = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        //let (pauli, _) = tupleArr[1];
        //Message($"{pauli}");
        //
        //let arrTuple = ([1,2], true);
        //let (vals, _) = arrTuple;
        //Message($"{vals[1]}, {vals}");
        //
        //let arrArr = [[PauliX, PauliZ], [], [PauliY], [PauliI]];
        //let pauliY = arrArr[2][0];
        //Message($"{arrArr}");
        //Message($"{arrArr[1]}, {arrArr[3]}, {pauliY}");

        return Zero;
    }
}

namespace Microsoft.Quantum.Intrinsic {

    function Message (arg : String) : Unit {
        body intrinsic;
    }
}
