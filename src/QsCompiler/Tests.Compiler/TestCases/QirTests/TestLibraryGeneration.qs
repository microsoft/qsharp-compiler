// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    operation Foo(c1 : Int, c2 : Bool) : Bool {
        return Bar(c1 > 0 and c2);
    }

    operation Bar<'T>(a1 : 'T) : 'T {
        return a1;
    }

    operation Baz(q : Qubit) : Unit is Adj {
        H(q);
        T(q);
    }
}
