// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.Capability {
    operation LibraryGen0(q : Qubit) : Unit { }

    operation LibraryGen1(q : Qubit) : Unit {
        let r = One;
        if (r == One) {
            LibraryGen0(q);
        }
    }

    operation LibraryUnknown(q : Qubit) : Unit {
        let r = One;
        let isOne = r == One;
        if (isOne) {
            LibraryGen0(q);
        }
    }
}
