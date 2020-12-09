// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace Microsoft.Quantum.Simulation.QuantumProcessor.Extensions {
    operation ApplyIfZero<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit), zeroArg : 'T)) : Unit { }
    operation ApplyIfZeroA<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj), zeroArg : 'T)) : Unit is Adj { }
    operation ApplyIfZeroC<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl), zeroArg : 'T)) : Unit is Ctl { }
    operation ApplyIfZeroCA<'T>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl + Adj), zeroArg : 'T)) : Unit is Ctl + Adj { }

    operation ApplyIfOne<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit), oneArg : 'T)) : Unit { }
    operation ApplyIfOneA<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Adj), oneArg : 'T)) : Unit is Adj { }
    operation ApplyIfOneC<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Ctl), oneArg : 'T)) : Unit is Ctl { }
    operation ApplyIfOneCA<'T>(measurementResult : Result, (onResultOneOp : ('T => Unit is Ctl + Adj), oneArg : 'T)) : Unit is Ctl + Adj { }

    operation ApplyIfElseR<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit), zeroArg : 'T) , (onResultOneOp : ('U => Unit), oneArg : 'U)) : Unit { }
    operation ApplyIfElseRA<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Adj), oneArg : 'U)) : Unit is Adj { }
    operation ApplyIfElseRC<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Ctl), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Ctl), oneArg : 'U)) : Unit is Ctl { }
    operation ApplyIfElseRCA<'T,'U>(measurementResult : Result, (onResultZeroOp : ('T => Unit is Adj + Ctl), zeroArg : 'T) , (onResultOneOp : ('U => Unit is Adj + Ctl), oneArg : 'U)) : Unit is Ctl + Adj { }

    operation ApplyConditionally<'T,'U>(measurementResults : Result[], resultsValues : Result[], (onEqualOp : ('T => Unit), equalArg : 'T) , (onNonEqualOp : ('U => Unit), nonEqualArg : 'U)) : Unit { }
    operation ApplyConditionallyA<'T,'U>(measurementResults : Result[], resultsValues : Result[], (onEqualOp : ('T => Unit is Adj), equalArg : 'T) , (onNonEqualOp : ('U => Unit is Adj), nonEqualArg : 'U)) : Unit is Adj { }
    operation ApplyConditionallyC<'T,'U>(measurementResults : Result[], resultsValues : Result[], (onEqualOp : ('T => Unit is Ctl), equalArg : 'T) , (onNonEqualOp : ('U => Unit is Ctl), nonEqualArg : 'U)) : Unit is Ctl { }
    operation ApplyConditionallyCA<'T,'U>(measurementResults : Result[], resultsValues : Result[], (onEqualOp : ('T => Unit is Ctl + Adj), equalArg : 'T) , (onNonEqualOp : ('U => Unit is Ctl + Adj), nonEqualArg : 'U)) : Unit is Ctl + Adj { }
}

namespace SubOps {
    operation SubOp1() : Unit { }
    operation SubOp2() : Unit { }
    operation SubOp3() : Unit { }

    operation SubOpCA1() : Unit is Ctl + Adj { }
    operation SubOpCA2() : Unit is Ctl + Adj { }
    operation SubOpCA3() : Unit is Ctl + Adj { }
}

// =================================

// Basic Lift
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubOp1();
            SubOp2();
            SubOp3();
            let temp = 4;
            using (q = Qubit()) {
                let temp2 = q;
            }
        }
    }
}

// =================================

// Lift Loops
namespace Microsoft.Quantum.Testing.ClassicalControl {

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            for (index in 0 .. 3) {
                let temp = index;
            }

            repeat {
                let success = true;
            } until (success)
            fixup {
                let temp2 = 0;
            }
        }
    }
}

// =================================

// Don't Lift Single Call
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
        }
    }
}

// =================================

// Lift Single Non-Call
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            let temp = 2;
        }
    }
}

// =================================

// Don't Lift Return Statements
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
            return ();
        }
    }
}

// =================================

// All-Or-None Lifting
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation IfInvalid() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
            SubOp2();
            return ();
        } else {
            SubOp2();
            SubOp3();
        }
    }

    operation ElseInvalid() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
            SubOp2();
        } else {
            SubOp2();
            SubOp3();
            return ();
        }
    }

    operation BothInvalid() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
            SubOp2();
            return ();
        } else {
            SubOp2();
            SubOp3();
            return ();
        }
    }
}

// =================================

// ApplyIfZero And ApplyIfOne
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubOp1();
        }

        let temp = 0;

        if (r == One) {
            SubOp2();
        }
    }
}

// =================================

// Apply If Zero Else One
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            Bar(r);
        } else {
            SubOp1();
        }
    }
}

// =================================

// Apply If One Else Zero
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r = One;

        if (r == One) {
            Bar(r);
        } else {
            SubOp1();
        }
    }
}

// =================================

// If Elif
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubOp1();
        } elif (r == One) {
            SubOp2();
        } else {
            SubOp3();
        }
    }
}

// =================================

// And Condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero and r == One) {
            SubOp1();
        } else {
            SubOp2();
        }
    }
}

// =================================

// Or Condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero or r == One) {
            SubOp1();
        } else {
            SubOp2();
        }
    }
}

// =================================

// Don't Lift Functions
namespace Microsoft.Quantum.Testing.ClassicalControl {

    function Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubFunc1();
            SubFunc2();
            SubFunc3();
        }
    }

    function SubFunc1() : Unit { }
    function SubFunc2() : Unit { }
    function SubFunc3() : Unit { }
}

// =================================

// Lift Self-Contained Mutable
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            mutable temp = 3;
            set temp = 4;
        }
    }
}

// =================================

// Don't Lift General Mutable
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        mutable temp = 3;
        if (r == Zero) {
            set temp = 4;
        }
    }
}

// =================================

// Generics Support
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo<'A, 'B, 'C>() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubOp1();
            SubOp2();
        }
    }
}

// =================================

// Adjoint Support
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Provided() : Unit is Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }

    operation Self() : Unit is Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint self;
    }

    operation Invert() : Unit is Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint invert;
    }
}

// =================================

// Controlled Support
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Provided() : Unit is Ctl {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }

    operation Distribute() : Unit is Ctl {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled distribute;
    }
}

// =================================

// Controlled Adjoint Support - Provided
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation ProvidedBody() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }

    operation ProvidedAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }

    operation ProvidedControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }

    operation ProvidedAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        adjoint (...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }

        controlled adjoint (ctl, ...) {
            let b = One;

            if (b == One) {
                let temp1 = 0;
                let temp2 = 0;
                SubOpCA3();
            }
        }
    }
}

// =================================

// Controlled Adjoint Support - Distribute
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation DistributeBody() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        adjoint (...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }

        controlled adjoint distribute;
    }
}

// =================================

// Controlled Adjoint Support - Invert
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation InvertBody() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled adjoint invert;
    }

    operation InvertAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint invert;
    }

    operation InvertControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint invert;
    }

    operation InvertAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        adjoint (...) {
            let y = One;

            if (y == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }

        controlled adjoint invert;
    }
}

// =================================

// Controlled Adjoint Support - Self
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation SelfBody() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled adjoint self;
    }

    operation SelfControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOpCA3();
                SubOpCA1();
            }
        }

        controlled adjoint self;
    }
}

// =================================

// Within Block Support
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = One;
        within {
            if (r == Zero) {
                SubOpCA1();
                SubOpCA2();
            }
        } apply {
            if (r == One) {
                SubOpCA2();
                SubOpCA3();
            }
        }
    }
}

// =================================

// Arguments Partially Resolve Type Parameters
namespace Microsoft.Quantum.Testing.ClassicalControl {

    operation Bar<'Q, 'W> (q : 'Q, w : 'W) : Unit { }

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            Bar<Int, _>(1, 1.0);
        }
    }
}

// =================================

// Lift Functor Application
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            Adjoint SubOpCA1();
        }
    }
}

// =================================

// Lift Partial Application
namespace Microsoft.Quantum.Testing.ClassicalControl {

    operation Bar (q : Int, w : Double) : Unit { }

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            (Bar(1, _))(1.0);
        }
    }
}

// =================================

// Lift Array Item Call
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let f = [SubOp1];
        let r = Zero;
        if (r == Zero) {
            f[0]();
        }
    }
}

// =================================

// Lift One Not Both
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            SubOp1();
            SubOp2();
        }
        else {
            SubOp3();
        }
    }
}

// =================================

// Apply Conditionally
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r1 = Zero;
        let r2 = Zero;

        if (r1 == r2) {
            Bar(r1);
        }
        else {
            SubOp1();
        }
    }
}

// =================================

// Apply Conditionally With NoOp
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r1 = Zero;
        let r2 = Zero;

        if (r1 == r2) {
            Bar(r1);
        }
    }
}

// =================================

// Inequality with ApplyConditionally
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r1 = Zero;
        let r2 = Zero;

        if (r1 != r2) {
            Bar(r1);
        }
        else {
            SubOp1();
        }
    }
}

// =================================

// Inequality with Apply If One Else Zero
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r = Zero;

        if (r != Zero) {
            Bar(r);
        }
        else {
            SubOp1();
        }
    }
}

// =================================

// Inequality with Apply If Zero Else One
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Bar(r : Result) : Unit { }

    operation Foo() : Unit {
        let r = Zero;

        if (r != One) {
            Bar(r);
        }
        else {
            SubOp1();
        }
    }
}

// =================================

// Inequality with ApplyIfOne
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r != Zero) {
            SubOp1();
        }
    }
}

// =================================

// Inequality with ApplyIfZero
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r != One) {
            SubOp1();
        }
    }
}

// =================================

// Literal on the Left
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (Zero == r) {
            SubOp1();
        }
    }
}

// =================================

// Simple NOT condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (not (r == Zero)) {
            SubOp1();
        }
        else {
            SubOp2();
        }
    }
}

// =================================

// Outer NOT condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (not (r == Zero or r == One)) {
            SubOp1();
        }
        else {
            SubOp2();
        }
    }
}

// =================================

// Nested NOT condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero or not (r == One)) {
            SubOp1();
        }
        else {
            SubOp2();
        }
    }
}

// =================================

// One-Sided NOT condition
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (not (r == One)) {
            SubOp1();
        }
    }
}