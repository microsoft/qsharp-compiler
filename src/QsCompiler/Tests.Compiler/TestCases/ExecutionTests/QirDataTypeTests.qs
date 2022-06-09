// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.ExecutionTests {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Canon;

    newtype MyUnit = Unit;
    newtype MyTuple = (Item1 : Int, Item2 : Double);
    newtype MyNestedTuple = ((Item1 : Int, Item2 : Double), Item3 : Double);

    @EntryPoint()
    operation TestNativeTypeHandling() : Unit {
        let arr1 = [1,2,3];
        Message($"{arr1}");
        
        mutable sum = 0;
        for item in arr1 {
            set sum += item;
        }
        
        let arr2 = [6, size = 3];
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
        let updatedArrArr2 = arrArr w/ 1 <- [-1,-2,-3];
        let updatedArrArr3 = arrArr2 w/ 0 <- [];
        let updatedArrArr4 = arrArr2 w/ 1 <- [PauliX, PauliX, PauliX];
        Message($"{updatedArrArr1}");
        Message($"{updatedArrArr2}");
        Message($"{updatedArrArr3}");
        Message($"{updatedArrArr4}");

        use (qs1, qs2, q) = (Qubit[2], Qubit[1], Qubit());
        let qubitArrArr = [qs1, [], qs2, [q]];
        Message($"{qubitArrArr w/ 0 <- []}");
        Message($"{qubitArrArr w/ 1 <- [q, q, q]}");
        
        let arrArrArr = [[[2], [1,0]], [], [[], [3]], [[0,1,2]]];
        Message($"{arrArrArr[0][1][1]}, {arrArrArr[1]}, {arrArrArr[3][0][2]}");
        Message($"{arrArrArr w/ 3 <- [[1,2,3,4], []]}");
        Message($"{arrArrArr w/ 2 .. 3 <- [[], []]}");
    }
}
