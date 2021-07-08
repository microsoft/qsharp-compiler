// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    operation Main() : Unit {
        let _ = TestType((PauliI, 0), 0.);
    }
}
