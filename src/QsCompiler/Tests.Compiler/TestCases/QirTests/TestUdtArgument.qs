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


    newtype TestType1 = Int;
    newtype TestType2 = (Pauli, I : Int);
    newtype TestType3 = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    operation TestUdtArgument () : (Int) //, ((Pauli, Int), Double)) // TODO: returning tuples is not yet implemented
    {
        let udt1 = Build1(TestType1);
        let udt2 = Build2(TestType2(PauliX, _));
        //let udt3 = (TestType3((PauliX, 1), 2.0)); // TODO: there is currently a bug in the partial application mapping
        return (udt1!); //, udt3!);
    }
}
