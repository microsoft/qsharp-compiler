// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.AzureSamples {
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation MeasureBell() : Bool {

        use (alice, bob) = (Qubit(), Qubit());
        H(alice);
        CNOT(alice, bob);
        let (m1, m2) = (M(alice), M(bob));
        return m1 == m2;
    }
}
