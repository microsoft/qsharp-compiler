// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    function ReturnTuple (arg : String) : (String, (Int, Double)){
        return (arg, (1, 0.));
    }

    operation TestLocalCallables () : (String, Double) {
        let fct = ReturnTuple;
        let (str, (_, val)) = fct("");
        return (str, val);
    }
}
