// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.Capability {
    operation LibraryBqf(q : Qubit) : Unit { }

    operation LibraryBmf(q : Qubit) : Unit {
        let r = One;
        if (r == One) {
            LibraryBqf(q);
        }
    }

    operation LibraryBmfWithNestedCall(q : Qubit) : Unit {
        let r = One;
        if (r == One) {
            LibraryBmf(q);
        }
    }

    operation LibraryFull(q : Qubit) : Unit {
        let r = One;
        let isOne = r == One;
        if (isOne) {
            LibraryBqf(q);
        }

        mutable x = 0;
        if (r == One) {
            set x = 1;
            return ();
        }
    }

    operation LibraryFullWithNestedCall(q : Qubit) : Unit {
        let r = One;
        let isOne = r == One;
        if (isOne) {
            LibraryFull(q);
        }
    }
}
