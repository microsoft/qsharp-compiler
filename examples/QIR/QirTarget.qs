// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Instructions {
    @Intrinsic("h")
    operation PhysH (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("s")
    operation PhysS (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("z")
    operation PhysZ (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("x")
    operation PhysX (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("rx")
    operation PhysRx (theta : Double, qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("rz")
    operation PhysRz (theta : Double, qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("cnot")
    operation PhysCNOT (control : Qubit, target : Qubit) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("mz")
    operation PhysM (qb : Qubit) : Result
    {
        body intrinsic;
    }

    @Intrinsic("measure")
    operation PhysMeasure (bases : Pauli[], qubits : Qubit[]) : Result
    {
        body intrinsic;
    }

    @Intrinsic("intAsDouble")
    function IntAsDoubleImpl(i : Int) : Double
    {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Intrinsic {
    open Microsoft.Quantum.Instructions;

    @Inline()
    operation X(qb : Qubit) : Unit
    is Adj {
        body (...)
        {
            PhysX(qb);  
		}
        adjoint self;
	}

    @Inline()
    operation Z(qb : Qubit) : Unit
    is Adj {
        body (...)
        {
            PhysZ(qb);  
		}
        adjoint self;
	}

    @Inline()
    operation H(qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            PhysH(qb);  
		}
        adjoint self;
	}

    @Inline()
    operation S(qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            PhysS(qb);  
		}
        adjoint (...)
        {
            PhysS(qb);
            PhysZ(qb);
        }
	}

    @Inline()
    operation Mz(qb : Qubit) : Result
    {
        return PhysM(qb);  
	}

    @Inline()
    operation Measure(bases : Pauli[], qubits : Qubit[]) : Result
    {
        return PhysMeasure(bases, qubits);
    }

    operation MResetZ(qb : Qubit) : Result
    {
        let res = Mz(qb);
        if (res == One)
        {
            X(qb);
        }
        return res;
    }

    operation Reset(qb : Qubit) : Unit
    {
        if (Mz(qb) == One)
        {
            X(qb);
        }
    }

    @Inline()
    operation Rx(theta : Double, qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            PhysRx(theta, qb);  
		}
        adjoint (...)
        {
            PhysRx(-theta, qb);  
		}
	}

    @Inline()
    operation Rz(theta : Double, qb : Qubit) : Unit
    is Adj + Ctl {
        body  (...)
        {
            PhysRz(theta, qb);  
		}
        adjoint (...)
        {
            PhysRz(-theta, qb);  
		}
        controlled (ctls, ...)
        {
            PhysRz(theta / 2.0, qb);
            CNOT(ctls[0], qb);
            PhysRz(-theta / 2.0, qb);
            CNOT(ctls[0], qb);
        }
        controlled adjoint (ctls, ...)
        {
            PhysRz(-theta / 2.0, qb);
            CNOT(ctls[0], qb);
            PhysRz(theta / 2.0, qb);
            CNOT(ctls[0], qb);
        }
	}

    @Inline()
    operation CNOT(control : Qubit, target : Qubit) : Unit
    is Adj {
        body (...)
        {
            PhysCNOT(control, target);  
		}
        adjoint self;
	}

    @Inline()
    function IntAsDouble(i : Int) : Double
    {
        return IntAsDoubleImpl(i);
    }

    @Inline()
    function PI() : Double
    {
        return 3.14159265357989;
    }
}
