// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    newtype HasString = (
        Value: String,
        Data: Double[]
    );

    operation TestPendingRefCountIncreases(cond : Bool) : HasString {
        let s = HasString("<", [0.0]);
        let updated = s w/ Data <- [0.1, 0.2];
        if cond {
            return s;
        } else {
            return s;
        }
    }

    function TestRefCountsForItemUpdate(cond : Bool) : Unit {
        mutable ops = new Int[][5];
        if (cond) {
            set ops w/= 0 <- new Int[3];
        }
    }
}
