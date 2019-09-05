﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for function evaluation
namespace Microsoft.Quantum.Testing.Optimization.FunctionEval {
    operation Test () : (Int, Int, Int, Int, Double) {
        let b = f(1, 8) + g1(100);
        let (c, d, e) = (g2(3), g2(4), g2(5));
        let s = mySin(2.0);
        return (b, c, d, e, s);
    }

    function f (x : Int, w : Int) : Int {
        mutable y = 1;
        mutable z = new Int[5];
        set z = z w/ 0 <- x;
        while (z[0] > 0) {
            set y += w;
            set z = z w/ 0 <- z[0] / 2;
        }
        mutable b = 0;
        for (a in z) {
            set b += a;
        }
        return y + b;
    }

    function g1 (x : Int) : Int {
        if (x == 0) {
            return 0;
        }
        if (x == 1) {
            return 1;
        }
        return g1(x-1) + g1(x-2);
    }

    function g2 (x : Int) : Int {
        return x == 0 ? 0 | (x == 1 ? 1 | g2(x-1) + g2(x-2));
    }

    function mySin (x : Double) : Double {
        let y = ArcSinh(x);
        if (y == 0.0) {
            return 2.0;
        }
        return ArcSinh(y);
    }

    function ArcSinh (x : Double) : Double {
        body intrinsic;
    }
}