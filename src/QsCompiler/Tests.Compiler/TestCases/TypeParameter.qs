// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


// Identifier Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        Foo<_, Int, String>(1.0, 2, "Three");
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit { }
}

// =================================

// Adjoint Application Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        (Adjoint (Foo(1.0, 2, _)))("Three");
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit is Adj { }
}

// =================================

// Controlled Application Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main(qs : Qubit) : Unit {
        (Controlled (Foo(1.0, 2, _)))([qs], "Three");
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit is Ctl { }
}

// =================================

// Partial Application Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        (Foo(_, 3, "Hi"))(1.0);
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit { }
}

// =================================

// Sub-call Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        (Foo(1, 2, 3))(4);
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : ('C => Unit) { return Bar<'A, 'B, 'C>(a, b, _); }

    operation Bar<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit { }
}

// =================================

// Argument Sub-call Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        Foo(1.0, Bar(2), "Three");
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit { }

    operation Bar<'A>(a : 'A) : 'A { return a; }
}
