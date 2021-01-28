// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype LittleEndian = Qubit[];
    newtype Complex = (Double, Double);

    operation ApplyOp(dummy : Double, coefficients : Complex[], qubits : LittleEndian) : Unit is Adj + Ctl {
    }

    operation TestAccessCounts(coefficients : Complex[], qubits : LittleEndian) : Unit is Adj + Ctl {
        ApplyOp(0.0, coefficients, qubits);
    }
}
