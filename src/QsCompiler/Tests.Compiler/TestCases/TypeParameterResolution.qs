// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Basic Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo();
        Bar();
    }

    operation Foo() : Unit { }

    operation Bar() : Unit {
        Baz();
    }

    operation Baz() : Unit { }
}

// =================================

// Unrelated To Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit { }

    operation Bar() : Unit {
        Baz();
    }

    operation Baz() : Unit { }
}

// =================================

// Not Called With Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit { }

    operation NotCalled() : Unit { }
}

// =================================

// Not Called Without Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit { }

    operation NotCalled() : Unit { }
}

// =================================

// Unrelated Without Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit { }

    operation Bar() : Unit {
        Baz();
    }

    operation Baz() : Unit { }
}

// =================================

// Entry Point No Descendants
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit { }

    operation Foo() : Unit {
        Bar();
    }

    operation Bar() : Unit { }
}

// =================================

// Calls Entry Point From Entry Point
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit {
        Main();
    }
}

// =================================

// Entry Point Ancestor And Descendant
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo();
    }

    operation Foo() : Unit { }

    operation Bar() : Unit {
        Main();
    }
}

// =================================

// Concrete Graph has Concretizations
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Foo<Double>();
        Bar<String>();
    }

    operation Foo<'A>() : Unit { }

    operation Bar<'A>() : Unit {
        Foo<'A>();
    }
}

// =================================

// Concrete Graph Trims Specializations
namespace Microsoft.Quantum.Testing.TypeParameterResolution {

    @ EntryPoint()
    operation Main() : Unit {
        Adjoint FooAdj();
        using (q = Qubit()) {
            (Controlled FooCtl)([q], ());
            (Controlled Adjoint FooCtlAdj)([q], ());
        }
    }

    operation FooAdj() : Unit is Adj {
        body(...) {
            Unused();
        }

        adjoint(...) {
            BarAdj();
        }
    }

    operation FooCtl() : Unit is Ctl {
        body(...) {
            Unused();
        }

        controlled(ctl, ...) {
            BarCtl();
        }
    }

    operation FooCtlAdj() : Unit is Ctl+Adj {
        body(...) {
            Unused();
        }

        adjoint(...) {
            Unused();
        }

        controlled(ctl, ...) {
            Unused();
        }

        controlled adjoint(ctl, ...) {
            BarCtlAdj();
        }
    }

    operation BarAdj() : Unit { }
    operation BarCtl() : Unit { }
    operation BarCtlAdj() : Unit { }
    operation Unused() : Unit { }
}

// =================================

// Concrete Graph Double Reference Resolution
namespace Microsoft.Quantum.Testing.TypeParameterResolution {
    function Foo<'a>(x : 'a) : 'a {
        return x;
    }

    @EntryPoint()
    function Main() : Unit {
        let x = (Foo(Foo))(0);
    }
}

// =================================

// Concrete Graph Non-Call Reference Only Body
namespace Microsoft.Quantum.Testing.TypeParameterResolution {
    operation Foo() : Unit { }

    @EntryPoint()
    operation Main() : Unit {
        let f = Foo;
    }
}

// =================================

// Concrete Graph Non-Call Reference With Adjoint
namespace Microsoft.Quantum.Testing.TypeParameterResolution {
    operation Foo() : Unit is Adj {
        body(...) { }
        adjoint(...) { }
    }

    @EntryPoint()
    operation Main() : Unit {
        let f = Foo;
    }
}

// =================================

// Concrete Graph Non-Call Reference With All
namespace Microsoft.Quantum.Testing.TypeParameterResolution {
    operation Foo() : Unit is Ctl+Adj {
        body(...) { }
        controlled(ctl, ...) { }
        adjoint(...) { }
        controlled adjoint (ctl, ...) { }
    }

    @EntryPoint()
    operation Main() : Unit {
        let f = Foo;
    }
}
