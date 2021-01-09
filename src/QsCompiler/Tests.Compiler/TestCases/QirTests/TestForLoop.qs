// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    function TestNestedLoops () : Double {

        mutable energy = 0.0;

        for (i in 0 .. 10) {
            for (j in 5 .. -1 .. 0) {
                set energy += 0.5;
            }
        }

        return energy;
    }
}
