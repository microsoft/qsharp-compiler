// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    @EntryPoint()
    function TestStrings(a : Int, b : Int, arr : Int[]) : String
    {
        let x = $"a is {a} \\ \r\n";
        let y = $"a+b is {a+b}";
        let z = $"{y}";
        let i = $"Constant double {1.2} bool {true} Pauli {PauliX} Result {One} BigInt {1L} Range {0..3}";
        let data = $"{arr}";
        let res = $"a+b is {x}";
        let defaultArr = new String[1];
        let strArr = ["hello", size = 1];
        return "";
    }
}
