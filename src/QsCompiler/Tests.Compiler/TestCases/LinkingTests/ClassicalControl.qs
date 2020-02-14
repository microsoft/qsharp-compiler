// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


namespace SubOps {
    operation SubOp1() : Unit is Adj + Ctl { }
    operation SubOp2() : Unit is Adj + Ctl { }
    operation SubOp3() : Unit is Adj + Ctl { }
}

// =================================

// Basic Hoist
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

// Hoist Loops
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

// Don't Hoist Single Call
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

// Hoist Single Non-Call
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

// Don't Hoist Return Statements
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

// All-Or-None Hoisting
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
            SubOp2();
            SubOp3();
        }

        let temp = 0;

        if (r == One) {
            SubOp2();
            SubOp3();
            SubOp1();
        }
    }

}

// =================================

// Apply If Zero Else One
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;

        if (r == Zero) {
            SubOp1();
            SubOp2();
        } else {
            SubOp2();
            SubOp3();
        }
    }

}

// =================================

// Apply If One Else Zero
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = One;

        if (r == One) {
            SubOp1();
            SubOp2();
        } else {
            SubOp2();
            SubOp3();
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
            SubOp2();
        } elif (r == One) {
            SubOp3();
            SubOp1();
        } else {
            SubOp2();
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
            SubOp2();
        } else {
            SubOp2();
            SubOp3();
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
            SubOp2();
        } else {
            SubOp2();
            SubOp3();
        }
    }

}

// =================================

// Don't Hoist Functions
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

// Hoist Self-Contained Mutable
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

// Don't Hoist General Mutable
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
                SubOp1();
                SubOp2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOp2();
                SubOp3();
            }
        }
    }

    operation Self() : Unit is Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        adjoint self;
    }

    operation Invert() : Unit is Adj {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOp1();
                SubOp2();
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
                SubOp1();
                SubOp2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOp2();
                SubOp3();
            }
        }
    }

    operation Distribute() : Unit is Ctl {
        body (...) {
            let r = Zero;

            if (r == Zero) {
                SubOp1();
                SubOp2();
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
                SubOp1();
                SubOp2();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
            }
        }
    }

    operation ProvidedAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
            }
        }
    }

    operation ProvidedControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        controlled adjoint (ctl, ...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
            }
        }
    }

    operation ProvidedAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        adjoint (...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
            }
        }

        controlled adjoint (ctl, ...) {
            let b = One;

            if (b == One) {
                let temp1 = 0;
                let temp2 = 0;
                SubOp3();
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
                SubOp1();
                SubOp2();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        adjoint (...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        controlled adjoint distribute;
    }

    operation DistributeAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }

        controlled (ctl, ...) {
            let w = One;

            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }

        adjoint (...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
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
                SubOp1();
                SubOp2();
            }
        }
    
        controlled adjoint invert;
    }
    
    operation InvertAdjoint() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }
    
        adjoint (...) {
            let w = One;
    
            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }
    
        controlled adjoint invert;
    }
    
    operation InvertControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }
    
        controlled (ctl, ...) {
            let w = One;
    
            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }
    
        controlled adjoint invert;
    }
    
    operation InvertAll() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }
    
        controlled (ctl, ...) {
            let w = One;
    
            if (w == One) {
                SubOp3();
                SubOp1();
            }
        }
    
        adjoint (...) {
            let y = One;
    
            if (y == One) {
                SubOp2();
                SubOp3();
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
                SubOp1();
                SubOp2();
            }
        }
    
        controlled adjoint self;
    }
    
    operation SelfControlled() : Unit is Ctl + Adj {
        body (...) {
            let r = Zero;
    
            if (r == Zero) {
                SubOp1();
                SubOp2();
            }
        }
    
        controlled (ctl, ...) {
            let w = One;
    
            if (w == One) {
                SubOp3();
                SubOp1();
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
                SubOp1();
                SubOp2();
            }
        } apply {
            if (r == One) {
                SubOp2();
                SubOp3();
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

// Hoist Functor Application
namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    operation Foo() : Unit {
        let r = Zero;
        if (r == Zero) {
            Adjoint SubOp1();
        }
    }
}

// =================================

// Hoist Partial Application
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

// Hoist Array Item Call
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