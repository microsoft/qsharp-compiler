// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// =================================

// With Return Value
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () => 0;
    }
}

// =================================

// Without Return Value
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () => ();
    }
}

// =================================

// Call Valued Callable
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Bar() : Int {
        return 0;
    }

    operation Foo() : Unit {
        let lambda = () => Bar();
    }
}

// =================================

// Call Unit Callable
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Bar() : Unit { }

    operation Foo() : Unit {
        let lambda = () => Bar();
    }
}

// =================================

// Call Valued Callable Recursive
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Int {
        let lambda = () => Foo();
        return 0;
    }
}

// =================================

// Call Unit Callable Recursive
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () => Foo();
    }
}

// =================================

// Use Closure
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let w = 0;
        let x = 0.0;
        let y = "Zero";
        let z = Zero;

        let lambda = () => (x, y, z);
    }
}

// =================================

// With Lots of Params
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda1 = (a => ())(0);
        let lambda2 = ((a) => ())(0);
        let lambda3 = ((a, b) => ())(0, 0.0);
        let lambda4 = ((a, b, c) => ())(0, 0.0, "Zero");
        let lambda5 = ((a, (b, c)) => ())(0, (0.0, "Zero"));
        let lambda6 = (((a, (b), c)) => ())(0, 0.0, "Zero");
        let lambda7 = (((a, b), c) => ())((0, 0.0), "Zero");
        let lambda8 = (((a, b, c)) => ())(0, 0.0, "Zero");
    }
}

// =================================

// Use Closure With Params
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let w = 0;
        let x = 0.0;
        let y = "Zero";
        let z = Zero;

        let lambda1 = (a => (x, y, z))(0);
        let lambda2 = ((a) => (x, y, z))(0);
        let lambda3 = ((a, b) => (x, y, z))(0, 0.0);
        let lambda4 = ((a, b, c) => (x, y, z))(0, 0.0, "Zero");
        let lambda5 = ((a, (b, c)) => (x, y, z))(0, (0.0, "Zero"));
        let lambda6 = (((a, (b), c)) => (x, y, z))(0, 0.0, "Zero");
        let lambda7 = (((a, b), c) => (x, y, z))((0, 0.0), "Zero");
        let lambda8 = (((a, b, c)) => (x, y, z))(0, 0.0, "Zero");
    }
}

// =================================

// Function Lambda
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () -> 0;
    }
}

// =================================

// With Type Parameters
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit {
        let lambda = () => (c, a);
    }
}

// =================================

// With Nested Lambda Call
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () => (() => 0)();
    }
}

// =================================

// With Nested Lambda
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = () => () => 0;
    }
}

// =================================

// Functor Support Basic Return
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda1 = () => 0;
        let lambda2 = () => ();
    }
}

// =================================

// Functor Support Call
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation BarInt() : Int {
        return 0;
    }

    operation Bar() : Unit { }
    operation BarAdj() : Unit is Adj { }
    operation BarCtl() : Unit is Ctl { }
    operation BarAdjCtl() : Unit is Adj + Ctl { }

    operation Foo() : Unit {
        let lambda1 = () => BarInt();
        let lambda2 = () => Bar();
        let lambda3 = () => BarAdj();
        let lambda4 = () => BarCtl();
        let lambda5 = () => BarAdjCtl();
    }
}

// =================================

// Functor Support Lambda Call
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation BarAdj() : Unit is Adj { }
    operation BarCtl() : Unit is Ctl { }
    operation BarAdjCtl() : Unit is Adj + Ctl { }

    operation Foo() : Unit {
        let lambda1 = () => (() => 0)();
        let lambda2 = () => (() => BarAdj())();
        let lambda3 = () => (() => BarCtl())();
        let lambda4 = () => (() => BarAdjCtl())();
    }
}

// =================================

// Functor Support Recursive
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        (() => Foo())();
    }

    operation FooAdj() : Unit is Adj {
        (() => FooAdj())();
    }

    operation FooCtl() : Unit is Ctl {
        (() => FooCtl())();
    }

    operation FooAdjCtl() : Unit is Adj + Ctl {
        (() => FooAdjCtl())();
    }
}

// =================================

// With Missing Params
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda1 = (_ => ())();
        let lambda2 = (_ => ())(0);
        let lambda3 = (_ => ())(0, 0.0);
        let lambda4 = ((_, _) => ())(0, 0.0);
        let lambda5 = ((x, _) => ())("Zero", (0, 0.0));
        let lambda6 = ((x, _, _) => ())("Zero", 0, 0.0);
    }
}

// =================================

// Use Parameter Single
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = x => x;
        let result = lambda(0);
    }
}

// =================================

// Use Parameter Tuple
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = (x, y) => (y, x);
        let result = lambda(0.0, 0);
    }
}

// =================================

// Use Parameter and Closure
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let a = 0;
        let lambda = x => (a, x);
        let result = lambda(0.0);
    }
}

// =================================

// Use Parameter with Missing Params
namespace Microsoft.Quantum.Testing.LambdaLifting {
    operation Foo() : Unit {
        let lambda = (x, _, _) => x;
        let result = lambda(0, Zero, "Zero");
    }
}
