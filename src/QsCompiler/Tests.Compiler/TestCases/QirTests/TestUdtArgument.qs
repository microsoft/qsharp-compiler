// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    // TODO: using the type parameterized version for Build instead current runs into a bug
    // function Build<'T>(build: (Int -> 'T)) : 'T
    // {
    //     return build(1);
    // }

    function Build1(build: (Int -> TestType1)) : TestType1
    {
        return build(1);
    }

    function Build2(build: (Int -> TestType2)) : TestType2
    {
        return build(1);
    }
    
    function Build3(build: (Int -> TestType3)) : TestType3
    {
        return build(1);
    }


    // TODO: WRITE A TEST THAT USES THE RETURNED UDT WITH MORE THAN ONE ITME
    // TODO: ADD A TEST THAT CHECKS THAT EACH VALUE IS PROPERLY PUSHED TO THE VALUE STACK
    // TODO: TEST UDT AS INNER TUPLE ITEM IN RETURN VALUE

    newtype TestType1 = Int;
    newtype TestType2 = (Pauli, I : Int);
    newtype TestType3 = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    operation TestUdtArgument () : (Int, (Pauli, Int))
    {
        let udt1 = Build1(TestType1);
        let udt2 = Build2(TestType2(PauliX, _));
        // TODO: there is currently a bug in the partial application mapping
        //let udt3 = (TestType3((PauliX, 1), 2.0));
        return (udt1!, udt2!);
    }
}
