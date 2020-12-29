// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype Complex = (Re : Double, Im : Double);
    newtype TestType = ((Pauli, I : Int), D : Double);

    function TestUdtConstructor() : TestType
    {
        let args = (1.,2.);
        let complex = Complex(args);
        return TestType((PauliX, 1), 2.0);
    }
}
