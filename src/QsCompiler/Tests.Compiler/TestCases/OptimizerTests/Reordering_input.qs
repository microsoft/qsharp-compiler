// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for statement reordering
namespace Microsoft.Quantum.Testing.Optimization.Reordering {
    operation Test (x : Int) : Unit {
        let y = x + 2;
        f(y);
        let z = y + 2;
        f(z);
    }

    operation f (n : Int) : Unit {
        body intrinsic;
    }
}