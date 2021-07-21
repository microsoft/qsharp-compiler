// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Convert {
    
    function IntAsDouble(a : Int) : Double {
        body intrinsic;
    }

    function DoubleAsInt(a : Double) : Int {
        body intrinsic;
    }
    
    function IntAsBigInt(a : Int) : BigInt {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Math {
    
    function Truncate (a : Double) : Int {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Math;

    function TestBuiltIn (arg : Int) : (Double, Int, BigInt, Int) {
        let d = IntAsDouble(arg);
        let i = DoubleAsInt(d);
        let bi = IntAsBigInt(arg);
        let t = Truncate(d);
        let range = 5 .. -2 .. 0;
        let rev = RangeReverse(range);
        return (d, i, bi, t);
    }

    @EntryPoint()
    function Main() : Unit {
        let _ = TestBuiltIn(0);
    }
}
