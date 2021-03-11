// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Instructions {

    operation S (qb : Qubit) : Unit {
        body intrinsic;
    }

    operation Rx (theta : Double, qb : Qubit) : Unit {
        body intrinsic;
    }

    operation Rz (theta : Double, qb : Qubit) : Unit {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Intrinsic {

    open Microsoft.Quantum.Targeting;
    open Microsoft.Quantum.Instructions as Phys;
    
    @Inline()
    function PI() : Double
    {
        return 3.14159265357989;
    }
    
    function IntAsDouble(i : Int) : Double {
        body intrinsic;
    }

    operation X(qb : Qubit) : Unit
    is Adj {
        body intrinsic;
        adjoint self;
	}

    operation Z(qb : Qubit) : Unit
    is Adj {
        body intrinsic;
        adjoint self;
	}

    operation H(qb : Qubit) : Unit
    is Adj {
        body intrinsic;
        adjoint self;
	}

    operation T(qb : Qubit) : Unit 
    is Adj {
        body intrinsic;
	}

    operation CNOT(control : Qubit, target : Qubit) : Unit
    is Adj {
        body intrinsic;
        adjoint self;
	}

    @TargetInstruction("mz")
    operation M(qb : Qubit) : Result {
        body intrinsic;
	}

    operation Measure(bases : Pauli[], qubits : Qubit[]) : Result {
        body intrinsic;
    }

    operation MResetZ(qb : Qubit) : Result
    {
        let res = M(qb);
        if (res == One)
        {
            X(qb);
        }
        return res;
    }

    @Inline()
    operation S(qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            Phys.S(qb);  
		}
        adjoint (...)
        {
            Phys.S(qb);
            Z(qb);
        }
	}

    @Inline()
    operation Rx(theta : Double, qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            Phys.Rx(theta, qb);  
		}
        adjoint (...)
        {
            Phys.Rx(-theta, qb);  
		}
	}

    @Inline()
    operation Rz(theta : Double, qb : Qubit) : Unit
    is Adj + Ctl {
        body  (...)
        {
            Phys.Rz(theta, qb);  
		}
        adjoint (...)
        {
            Phys.Rz(-theta, qb);  
		}
        controlled (ctls, ...)
        {
            Phys.Rz(theta / 2.0, qb);
            CNOT(ctls[0], qb);
            Phys.Rz(-theta / 2.0, qb);
            CNOT(ctls[0], qb);
        }
        controlled adjoint (ctls, ...)
        {
            Phys.Rz(-theta / 2.0, qb);
            CNOT(ctls[0], qb);
            Phys.Rz(theta / 2.0, qb);
            CNOT(ctls[0], qb);
        }
	}
}