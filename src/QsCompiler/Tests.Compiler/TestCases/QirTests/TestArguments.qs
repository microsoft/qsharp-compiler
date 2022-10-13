// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {


    // This file tests argument and output handling when targeting quantum backends.
    // The following data types are not supported for both parameters and return types:
    // String, BigInt, Qubit, callable types, and user defined types

    @EntryPoint()
    operation BasicParameterTest(
        arr : Pauli[], res : Result, range : Range, (cnt : Int, b : Bool, (d: Double)))
    : (Pauli[], Result, Range, (Int, Bool, (Double))) {

        return (arr, res, range, (cnt, b, d));
    }
}
