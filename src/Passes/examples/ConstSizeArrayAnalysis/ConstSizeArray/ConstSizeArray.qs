// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Example {
    @EntryPoint()
    operation Main() : Int
    {
        return QuantumFunction(10);
    }

    operation QuantumFunction(nQubits : Int) : Int {
        use qubits = Qubit[nQubits];

        return 0;
    }
}
