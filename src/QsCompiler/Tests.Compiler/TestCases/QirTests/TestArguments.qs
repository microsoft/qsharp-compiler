// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    // This file tests argument and output handling when targeting quantum backends.
    // The following data types are not supported for both parameters and return types:
    // String, BigInt, Qubit, callable types, and user defined types

    @EntryPoint()
    operation BasicParameterTest(
        arr : Int[], res : Result, range : Range, (p : Pauli, b : Bool, (d: Double)))
    : (Int[], Result, Range, (Pauli, Bool, (Double))) {

        return (arr, res, range, (p, b, d));
    }

    @EntryPoint()
    function ArrayArgumentTest(arr : Int[]) : Unit {

        mutable squares = arr w/ 0 <- 5;
        for idx in 0 .. Length(arr) - 1 {
            set squares w/= idx <- arr[idx] * arr[idx];
        }

        // not supported when executing on quantum hardware:
        // let _ = [arr, []];
        // let _ = [0, size = Length(arr)];
    }
}
