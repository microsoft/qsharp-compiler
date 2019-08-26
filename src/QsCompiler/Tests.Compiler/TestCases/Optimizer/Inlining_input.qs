// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for operation inlining
namespace Microsoft.Quantum.Testing.Optimization.Inlining {

    operation Test (q : Qubit) : Unit {
        f(q, 5);
    }

    function T (q : Qubit) : Unit {
        body intrinsic;
    }

    function f (q : Qubit, n : Int) : Unit {
        if (n == 0) {
            // Do nothing
        }
        elif (n == 1) {
            T(q);
        }
        else {
            f(q, n-1);
            f(q, n-2);
        }
    }
}