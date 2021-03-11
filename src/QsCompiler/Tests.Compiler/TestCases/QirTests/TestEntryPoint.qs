// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    @EntryPoint()
    operation TestEntryPoint(
        arr : Pauli[], str : String, res : Result, range : Range, (cnt : Int, b : Bool))
    : (Pauli[], String, Result, Range, (Int, Bool)) {

        mutable sum = 0.0;
        mutable flag = b;
        for pauli in arr
        {
            let value = pauli == PauliI ? 0. | 1.;
            set sum = sum + (flag ? value | -value);
            set flag = not flag;
        }

        return (arr, str, res, range, (cnt, b));
    }
}
