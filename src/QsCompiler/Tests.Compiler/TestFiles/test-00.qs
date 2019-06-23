// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing {

    newtype BigEndianRegister = Qubit[];
    newtype OtherType         = (Int, Qubit[]); 
    newtype OtherTuple        = (OtherType, Int); 
    newtype QubitAlias        = Qubit;

    // First comment
    operation CNOT (q1 : Qubit, q2 : Qubit) : Unit 
	is Ctl + Adj {
        body intrinsic;
    }

    operation M (q : Qubit) : Result {
        body intrinsic;
    }
    
    operation op0 (q : Qubit) : Result { 

        // Body comment
        repeat {
            mutable res = new Result[2];
            let o = [1.,2.,-4.2];
        } until (res[0] == One)
        fixup {
            set res w/= 0 <- One;
        }

        using (qs = Qubit [4]) {
			return One;
        }
    }

    function pickM (p : Pauli, d : Double) : (Qubit => Result) {

        // Function comment
        let twice = d * 2.0;

        if (twice > 1.0) {
            if (p == PauliZ) {
                return op0;
            }
        }

        return (M);
    }



    operation op1 (q1 : Qubit, r1 : BigEndianRegister) : Unit 
	is Ctl + Adj {   
        adjoint self;
        body (...) {
            let q2 = r1![0];
            Adjoint CNOT (q1, q2);
            for (item in r1!) {
                for (j in 0 .. 2 .. 4) {
                    CNOT(q1, item);
                }
            }
        }    
    } 

    operation op2 (q2 : Qubit, n2 : Int, b2 : BigEndianRegister, q : QubitAlias) : Unit {   
        body (...) {
            op1(q2, b2);
            let op = pickM(PauliI, 0.02);
            let r = op (q2);

            if (r == One) {        
                for (i in 0 .. 1 .. 5)
                {
                    if ((i % 2) == 0) {
                        let c = q;
                        let m = op0(b2![i]);
                        op1 (q!, b2);
                    }
                }
            } 
			else {
                fail "r is supposed to be Zero";
            }
        }

        adjoint (...) {
            // Adjoint comment
            let r = op0 (q2);
            let op = pickM(PauliZ, (0.05 * 4.0));

            if (r == One) {        
                for (i in 5 .. -1 .. 0)
                {
                    let c = q;
                    let m = op0 (b2![i]);
                    Adjoint op1 (q!, b2);
                }
            }
            Adjoint op1 (q2, b2);
        }

        controlled (ctrls, ...) {

            Controlled op1 (ctrls, (q2, b2));
            let op = pickM(PauliX, (0.02 / 4.0));
            let r = op (q2);

            if (r == One) {        
                for (i in 0 .. 1 .. 5)
                {
                    let c = q;
                    let m = op0 (b2![i]);
                    Controlled op1 (ctrls, (q!, b2));
                }
            }
        }
    
        adjoint controlled (ctrls, ...) {
            Adjoint Controlled op1 (ctrls, (q2, b2));
        }
    } 
}