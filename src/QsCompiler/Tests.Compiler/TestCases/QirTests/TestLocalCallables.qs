// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    operation DoNothing() : Unit is Adj + Ctl {
        body intrinsic;
    }

    function ReturnTuple (arg : String) : (String, (Int, Double)){
        return (arg, (1, 0.));
    }

    operation TestLocalCallables () : (String, Double) {

        let arr = [DoNothing];
        Adjoint arr[0]();
        Controlled arr[0](new Qubit[0], ());
        arr[0]();

        let fct = ReturnTuple;
        let (str, (_, val)) = fct("");
        return (str, val);
    }
}
