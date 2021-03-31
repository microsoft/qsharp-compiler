// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestInts (a : Int, b : Int) : Int {
        let c = a > b ? a | b;
        let d = c * a - b / 7;
        let e = d >>> 3;
        let f = d ^ b;
        let g = (e &&& f) ||| 0xffff;
        return ~~~g;
    }
}
