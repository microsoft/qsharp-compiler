// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    function TestRefCountsForItemUpdate(cond : Bool) : Unit {
        mutable ops = new Int[][5];
        if (cond) {
            set ops w/= 0 <- new Int[3];
        }
    }
}
