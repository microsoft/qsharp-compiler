// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test1() : Unit {
        Test1Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test2() : Unit {
        Test2Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test3() : Unit {
        Test3Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Arrays;

    @ EntryPoint()
    operation TestTypeParameterResolutions(qs : Int[]) : Unit {
        let res = new Result[Length(qs)];
        let idx = IndexRange(qs);
    }
}
