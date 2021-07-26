// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Example {
    @EntryPoint()
    operation Main() : Int
    {
        QuantumFunction(3);
        QuantumFunction(10) ;
        return 0;
    }

    operation QuantumFunction(nQubits : Int) : Unit  {
        use qubits = Qubit[nQubits];
    }
}
