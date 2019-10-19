// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases where no optimizations can be done
namespace Microsoft.Quantum.Testing.Optimization.NoOp {
    operation Test (x : Int) : Unit {
        mutable y = 0;
        for (i in 0..x) {
            f(y + i);
            set y += 1;
        }
    }

    operation f (n : Int) : Unit {
        body intrinsic;
    }
}