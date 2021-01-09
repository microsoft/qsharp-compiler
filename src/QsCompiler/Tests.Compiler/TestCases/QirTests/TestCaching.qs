// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation TestCaching (arr : Int[]) : Int {

        using (q = Qubit()) {
            H(q);

            if (M(q) == Zero) {
                return Length(arr);
            }
        }

        let pad = Length(arr) < 10 ? 
            new Int[10] | arr;
        return Length(pad);
    }
}
