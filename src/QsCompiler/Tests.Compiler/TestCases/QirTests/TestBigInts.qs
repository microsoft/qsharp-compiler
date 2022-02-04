// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    function TestBigInts (a : BigInt, b : BigInt) : BigInt
    {
        let c = a > b ? a | b;
        let d = c * a - b / 7L;
        let e = d >>> 3;
        let f = d ^ 5;
        let g = (e &&& f) ||| 0xffffL;
        return ~~~g;
    }

    @EntryPoint()
    function Main() : Unit {
        let _ = TestBigInts(0L, 0L);
        let N1 = 12345L;
        let N2 = 1522605027922533360535618378132637429718068114961380688657908494580122963258952897654000350692006139L;
        let _ = TestBigInts(N1, N2);
    }
}
