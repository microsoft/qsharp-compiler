// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    @EntryPoint()
    operation Main() : Int {
        use register = Qubit[5];
        mutable count = 0;
        repeat {
            let results = MultiM(register);
            if results[0] == Zero {
                set count += 1;
            }
        }
        until count < 5;
        return count;
    }

    operation MultiM(targets : Qubit[]) : Result[] {
        body intrinsic;
    }
}
