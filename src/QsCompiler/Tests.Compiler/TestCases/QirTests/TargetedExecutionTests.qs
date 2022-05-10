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
        let tupleArr2 = [(PauliX, 0), (PauliZ, 1), (PauliY, 2)];
        let (pauli2, _) = tupleArr2[1];
        Message($"{pauli}, {pauli2}");

        let arrTuple = ([1,2], true);
        let (vals, _) = arrTuple;
        Message($"{vals[1]}, {vals}");

        let arrArr = [[2, 1], [], [3], [0]];
        let pauliY = arrArr[2][0];
        Message($"{arrArr}");
        Message($"{arrArr[1]}, {arrArr[3]}, {pauliY}");
        
        let arrArr2 = [[PauliX, PauliZ], [], [PauliY], [PauliI]];
        let pauliY2 = arrArr2[2][0];
        Message($"{arrArr2}");
        Message($"{arrArr2[1]}, {arrArr2[3]}, {pauliY2}");

        let updatedArrArr1 = arrArr w/ 0 <- [];
        let updatedArrArr2 = updatedArrArr1 w/ 1 <- [1,2,3];
        Message($"{updatedArrArr1}");
        Message($"{updatedArrArr2}");

        // TODO: write tests for array of array of array

        return Zero;
    }
}

namespace Microsoft.Quantum.Intrinsic {

    function Message (arg : String) : Unit {
        body intrinsic;
    }
}
