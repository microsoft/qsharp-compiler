// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace test5 {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;

    operation AllocateQubitTest () : Unit {
        
        using (qs = Qubit[1]) {
            Assert([PauliZ], [qs[0]], Zero, "Newly allocated qubit must be in |0> state");
        }
            
        Message("Test passed");
    }
}