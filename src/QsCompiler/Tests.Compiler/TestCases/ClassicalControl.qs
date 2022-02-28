// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace SubOps {
    operation SubOp1() : Unit { }
    operation SubOp2() : Unit { }
    operation SubOp3() : Unit { }

    operation SubOpCA1() : Unit is Ctl + Adj { }
    operation SubOpCA2() : Unit is Ctl + Adj { }
    operation SubOpCA3() : Unit is Ctl + Adj { }
}

namespace Microsoft.Quantum.Testing.General {
    operation Unitary (q : Qubit) : Unit {
        body intrinsic;
        adjoint auto;
        controlled auto;
        controlled adjoint auto;
    }

    operation M (q : Qubit) : Result {
        body intrinsic;
    }
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
            use q = Qubit() {
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
            for index in 0 .. 3 {
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

// =================================

// Don't Lift Classical Conditions
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let x = 0;

        if x == 1 {
            let y = 0;
            SubOp1();
        }

        if x == 2 {
            if x == 3 {
                let y = 0;
                SubOp1();
            }
        }
        else {
            let y = 0;
            SubOp1();
        }
    }
}

// =================================

// Mutables with Nesting Lift Both
namespace Microsoft.Quantum.Testing.ClassicalControl {
    operation Foo() : Unit {
        let r = Zero;
        
        if r == Zero {
            if r == One {
                mutable x = 0;
                set x = 1;
            }
        }
    }
}

// =================================

// Mutables with Nesting Lift Outer
namespace Microsoft.Quantum.Testing.ClassicalControl {
    operation Foo() : Unit {
        let r = Zero;

        if r == Zero {
            mutable x = 0;
            if r == One {
                set x = 1;
            }
        }
    }
}

// =================================

// Mutables with Nesting Lift Neither
namespace Microsoft.Quantum.Testing.ClassicalControl {
    operation Foo() : Unit {
        let r = Zero;
        
        mutable x = 0;
        if r == Zero {
            if r == One {
                set x = 1;
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Lift Inner
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        let x = 0;
        
        if x < 1 {
            if M(q) == Zero {
                mutable y = 0;
                set y = 1;
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Lift Outer
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        let x = 0;

        if M(q) == Zero {
            mutable y = 0;
            if x < 1 {
                set y = 1;
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Lift Outer With More Classic
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        let x = 0;
        
        if M(q) == Zero {
            mutable y = 0;
            if x < 1 {
                if x < 2 {
                    set y = 1;
                }
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Lift Middle
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        let x = 0;
        
        if x < 1 {
            if M(q) == Zero {
                mutable y = 0;
                if x < 2 {
                    if x < 3 {
                        set y = 1;
                    }
                }
            }
        }
    }
}

// =================================

// Nested Invalid Lifting
namespace Microsoft.Quantum.Testing.ClassicalControl {
    operation Foo() : Unit {
        let r = Zero;
        
        if r == Zero {
            if r == One {
                return ();
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Elif
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        let x = 0;
        
        if x < 1 {
            if M(q) == Zero {
                mutable y = 0;
                if x < 2 {
                    if x < 3 {
                        set y = 1;
                    }
                }
            }
        }
        elif M(q) == Zero {
            mutable y = 0;
            if x < 4 {
                if x < 5 {
                    set y = 2;
                }
            }
        }
        else {
            mutable y = 0;
            if M(q) == Zero {
                if x < 6 {
                    set y = 3;
                }
            }
        }
    }
}

// =================================

// Mutables with Classic Nesting Elif Lift First
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open Microsoft.Quantum.Testing.General;

    operation Foo() : Unit {
        use q = Qubit();
        Unitary(q);
        mutable x = 0;

        if x < 1 {
            if M(q) == Zero {
                mutable y = 0;
                if x < 2 {
                    if x < 3 {
                        set y = 1;
                    }
                }
            }
        }
        elif M(q) == Zero {
            if x < 4 {
                if x < 5 {
                    set x = 2;
                }
            }
        }
        else {
            mutable y = 0;
            if M(q) == Zero {
                if x < 6 {
                    set y = 3;
                }
            }
        }
    }
}

// =================================

// NOT Condition Retains Used Variables
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r1 = Zero;
        let r2 = Zero;
        if not (r1 == Zero and r2 == Zero) {
            let t1 = r1;
            let t2 = r2;
            SubOp1();
            SubOp2();
        }
    }
}

// =================================

// Minimal Parameter Capture
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let myInt = 1;
        let myDouble = 2.0;
        let unused = 3;
        let myString = "four";
        mutable myMutable = 5.0;

        let r = Zero;
        if r == Zero {
            let innerDouble = myDouble;
            let innerInt = myInt;
            let innerString = myString;
            let innerMutable = myMutable;
            SubOp1();
        }
    }
}
