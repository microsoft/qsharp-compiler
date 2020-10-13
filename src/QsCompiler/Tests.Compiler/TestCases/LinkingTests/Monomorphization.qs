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
    open Microsoft.Quantum.Testing.Generics;

    @ EntryPoint()
    operation Test4() : Unit {
        Test4Main();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Monomorphization {
    internal newtype MyInternalUDT = Int;
    newtype MyPublicUDT = String;
    
    // The first (Internal|Public) describes the access of the operation.
    // The second (Internal|Public) describes the type that will be passed in.
    internal operation InternalInternalFoo<'A>(x : 'A) : Unit { }
    internal operation InternalPublicFoo<'A>(x : 'A) : Unit { }
    operation PublicInternalFoo<'A>(x : 'A) : Unit { }
    operation PublicPublicFoo<'A>(x : 'A) : Unit { }

    @ EntryPoint()
    operation TestAccessModifiers() : Unit {
        InternalInternalFoo(MyInternalUDT(12));
        InternalPublicFoo(MyPublicUDT("Yes"));
        PublicInternalFoo(MyInternalUDT(3));
        PublicPublicFoo(MyPublicUDT("No"));
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
