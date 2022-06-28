// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.ExecutionTests {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Canon;

    @EntryPoint()
    operation TestTargetPackageHandling() : Unit {
    
        use qs = Qubit[2];
        Ignore(Measure([PauliX, PauliX], qs));
        ResetAll(qs);
    }
}
