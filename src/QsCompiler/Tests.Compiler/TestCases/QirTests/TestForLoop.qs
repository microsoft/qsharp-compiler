// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    newtype Energy = (Abs : Double, Name : String);

    function TestNestedLoops () : Energy {

        let name = "energy";
        mutable res = Energy(0.0, "");
        set res w/= Name <- name;

        mutable energy = 0.0;

        for (i in 0 .. 10) {
            for (j in 5 .. -1 .. 0) {
                set energy += 0.5;
            }
        }

        return res w/ Abs <- energy;
    }
}
