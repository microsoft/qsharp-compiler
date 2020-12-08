// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    // FIXME: FOR SOME REASON USING A GENERIC INSTEAD DOESN'T WORK...!
    //function Build<'T>(build: (Int -> 'T)) : 'T
    //{
    //    return build(1);
    //}

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
    operation TestUdtArgument () : (Int) //, ((Pauli, Int), Double)) // FIXME: IN ORDER TO PROCESS A TUPLE WE ACTUALLY NEED TO FILL IN THE TODO...
    {
        let udt1 = Build1(TestType1);
        let udt2 = Build2(TestType2(PauliX, _));
        //let udt3 = (TestType3((PauliX, 1), 2.0)); // FIXME: partial application args...
        return (udt1!); //, udt3!);
    }
}
