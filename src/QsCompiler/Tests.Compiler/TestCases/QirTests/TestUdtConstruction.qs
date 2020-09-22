// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Pauli, I : Int), D : Double);

    function TestUdtConstructor() : TestType
    {
        return TestType((PauliX, 1), 2.0);
    }
}
