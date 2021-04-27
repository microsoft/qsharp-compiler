// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    operation LengthCaching (vals : Int[]) : Int[] {
        return vals[...2...];
    }

    internal function Conditional(res : Result, (x : Double, y : Double)) : (Double, Double) {
        return res == Zero ? (x - 0.5, y) | (x, y + 0.5);
    }

    @EntryPoint()
    operation TestCaching (arr : Int[]) : Int {

        use q = Qubit() {
            H(q);
            let res = M(q);
            let _ = Conditional(res, (1., 2.));

            if (res == Zero) {
                return Length(arr);
            }
        }

        let pad = Length(arr) < 10 ? 
            new Int[10] | arr;
        let sliced = LengthCaching(pad);
        return Length(pad);
    }
}
