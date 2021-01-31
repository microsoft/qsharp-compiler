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

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Convert;

    function TestBuiltIn (arg : Int) : (Double, BigInt) {
        let d = IntAsDouble(arg);
        let bi = IntAsBigInt(arg);
        return (d, bi);
    }
}
