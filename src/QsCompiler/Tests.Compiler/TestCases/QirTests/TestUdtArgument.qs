// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function Build<'T>(build: (Int -> 'T)) : 'T
    {
        return build(1);
    }

    newtype TestType1 = Int;
    newtype TestType2 = (Pauli, I : Int);
    newtype TestType3 = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    operation TestUdtArgument () : (Int, (Pauli, Int))
    {
        let udt1 = Build(TestType1);
        let udt2 = Build(TestType2(PauliX, _));
        let udt3 = Build(TestType3((PauliX, _), 2.0));
        return (udt1!, udt2!);
    }
}
