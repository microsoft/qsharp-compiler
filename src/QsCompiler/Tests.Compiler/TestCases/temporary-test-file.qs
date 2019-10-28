// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

namespace Microsoft.Quantum.Testing.Monomorphization {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Extensions.Math;
    
	operation MyBasisChange<'Q>(q : Qubit, bar : 'Q) : Int {
        body (...) {
			mutable temp = new 'Q[3];
			set temp w/= 0 <- bar;
			set temp w/= 1 <- bar;
			set temp w/= 2 <- bar;
			let temp2 = bar;
			H(q);
			return 3;
        }
    }

    operation NotMain (q : Qubit) : Unit is Adj+Ctl {
        body (...) {
            X(q);
			OtherThing(q, 12);
			let temp = MyBasisChange(q, MyBasisChange(q, "twelve"));
			let temp2 = MyBasisChange(q, 12);
        }
        
        adjoint (...) {
			X(q);
		}

		controlled (control, ...) {
			Controlled X(control, q);
		}
    }
    
	operation OtherThing<'T>(q : Qubit, bar : 'T) : Unit {
        body (...) {
			let temp = bar;
			H(q);
			let thing = MyBasisChange(q, bar);
        }
    }

	operation Bar<'A,'B,'C>(a: 'A, b: 'B, c: 'C) : 'C {
		body (...) {
			let temp = c;
			using (q = Qubit()) {
				let temp2 = b;
				X(q);
				Reset(q);
				let temp3 = a;
			}
			return temp;
		}
	}

	operation Foo<'A, 'B>(a: 'A, b: 'B) : Unit {
		body (...) {
			using (q = Qubit()) {
				let temp = a;
				X(q);
				Reset(q);
				let temp2 = b;
			}
		}
	}

	operation Foo2<'A>() : 'A {
		body (...) {
			let temp = new 'A[1];
			return temp[0];
		}
	}

	operation Foo3<'A, 'B>(a: 'A, b: 'B) : Int {
		body (...) {
			using (q = Qubit()) {
				let temp = a;
				X(q);
				Reset(q);
				let temp2 = b;
			}
			return 12;
		}
	}

	operation Main() : Unit {
		body (...) {
			let temp = Foo<_, Double>(1, _);
			let temp2 = Foo<Int, Double>(1, _);
			//
			(Foo("Yes", _))(1.0);
			//
			(Foo(Foo("Yes","No"), _))(Foo(1.0, 2));
			//
			(Foo<_,_>((Foo<_,String>("Yes",_))("No"), _))((Foo<Double, _>(_, 2))(1.0));
			//
			Foo((),());
			Foo("","");
			Foo(1.0,2);
			//
			let temp3 = Foo2<Double>();
			let bar = Foo3<String, _>("Yes", Foo3<Double, _>(12.0, "No"));
		}
	}

}
