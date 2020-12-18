// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    operation X (q : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }

    operation CNOT (control : Qubit, target : Qubit) : Unit
    is Adj + Ctl {

        body (...) {
            Controlled X([control], target);
        }
        
        adjoint self;
    }

    @EntryPoint()
    operation GetEnergyHydrogenVQE() : Unit {
        using (aux = Qubit())
        {
            CNOT(aux, aux);
        }
    }
}
