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

// Use Lots of Params
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
