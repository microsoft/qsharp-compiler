// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for generic operations
namespace Microsoft.Quantum.Testing.Generics {

    operation Test1Main() : Unit {
        let temp = BasicGeneric<_, Double>(1, _);
        let temp2 = BasicGeneric<Int, Double>(1, _);

        (BasicGeneric("Yes", _))(1.0);

        (BasicGeneric(BasicGeneric("Yes","No"), _))(BasicGeneric(1.0, 2));

        (BasicGeneric<_,_>((BasicGeneric<_,String>("Yes",_))("No"), _))((BasicGeneric<Double, _>(_, 2))(1.0));

        BasicGeneric((),());
        BasicGeneric("","");
        BasicGeneric(1.0,2);

        let temp3 = NoArgsGeneric<Double>();
        let bar = (ReturnGeneric<String, _, _>("Yes", (ReturnGeneric<Double, _, _>(12.0, "No", _))(4), _))("Maybe");
    }

    // Tests that unused generics are removed
    operation NotUsed<'A, 'B>(a: 'A, b: 'B) : Int {
        using (q = Qubit()) {
            let temp = a;
            let temp2 = b;
        }
        return 12;
    }

    operation BasicGeneric<'A, 'B>(a: 'A, b: 'B) : Unit {
        let temp = a;
        using (q = Qubit()) {
            let temp2 = b;
        }
    }

    operation NoArgsGeneric<'A>() : 'A {
        let temp = new 'A[1];
        return temp[0];
    }

    operation ReturnGeneric<'A,'B,'C>(a: 'A, b: 'B, c: 'C) : 'C {
        let temp = c;
        using (q = Qubit()) {
            let temp2 = b;
            let temp3 = a;
        }
        return temp;
    }

    operation Test2Main () : Unit {
        using (q = Qubit()) {
            GenericCallsGeneric(q, 12);
            let temp = ArrayGeneric(q, ArrayGeneric(q, "twelve"));
            let temp2 = ArrayGeneric(q, 12);
        }
    }

    operation ArrayGeneric<'Q>(q : Qubit, bar : 'Q) : Int {
        mutable temp = new 'Q[3];
        set temp w/= 0 <- bar;
        set temp w/= 1 <- bar;
        set temp w/= 2 <- bar;
        let temp2 = bar;
        return 3;
    }

    operation GenericCallsGeneric<'T>(q : Qubit, bar : 'T) : Unit {
        let temp = bar;
        let thing = ArrayGeneric(q, bar);
    }

    operation Test3Main () : Unit {
        GenericCallsSpecializations("First", 12, ());
        Adjoint GenericCallsSpecializations(12.0, "Second", 4.0);
        using (q = Qubit[4]) {
            Controlled GenericCallsSpecializations(q[0..1], (12.0, "Third", q[2..3]));
        }
    }

    operation GenericCallsSpecializations<'A,'B,'C>(a : 'A, b : 'B, c : 'C) : Unit is Adj+Ctl {
        body (...) {
            BasicGeneric(a, b);
        }

        adjoint (...) {
            using (q = Qubit()) {
                let temp = ArrayGeneric(q, c);
            }
        }

        controlled (ctrl, ...) {
            BasicGeneric(b, c);
        }

        controlled adjoint (ctrl, ...) {
            BasicGeneric(ctrl, c);
        }
    }

    operation GenericCallsSelf<'A>() : Unit {
        GenericCallsSelf<'A>();
    }

    operation GenericCallsSelf2<'A>(x : 'A) : Unit {
        GenericCallsSelf2(x);
    }

    operation Test4Main () : Unit {
        GenericCallsSelf<Double>();
        GenericCallsSelf2(0.0);
    }
}
