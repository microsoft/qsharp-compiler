// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for arithmetic optimization
namespace Microsoft.Quantum.Testing.Optimization.Arithmetic {
    operation Test () : Int {
        let x = 5;
        let y = x + 3;
        let z = y * 5 % 4;
        let w = z > 1;
        let a = w ? 2 | y / 2;
        return a;
    }

    operation Test2 (x : Int) : Int {
        let y = ((x - 0) + (x / 1) * 1) + 0;
        return y;
    }
}