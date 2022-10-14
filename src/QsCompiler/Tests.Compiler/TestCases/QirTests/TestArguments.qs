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
}
