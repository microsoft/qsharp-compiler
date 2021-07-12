// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestPaulis (a : Pauli, b : Pauli) : Pauli
    {
        if (a == PauliX)
        {
            return PauliY;
        }
        return b;
    }
}
