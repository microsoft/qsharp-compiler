// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Convert {
    
    function IntAsDouble(a : Int) : Double {
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

    function TestBuiltIn (arg : Int) : (Double, BigInt, Int) {
        let d = IntAsDouble(arg);
        let bi = IntAsBigInt(arg);
        let i = Truncate(d);
        return (d, bi, i);
    }
}
