// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    function TestAccessors() : Int
    {
        let x = TestType((PauliX, 1), 2.0);
        let y = x::I;
        return y;
    }
}
