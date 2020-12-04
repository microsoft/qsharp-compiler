// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Instructions {
    open Microsoft.Quantum.Targeting;

    @TargetInstruction("h")
    operation PhysH (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("x")
    operation PhysX (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("s")
    operation PhysS (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("t")
    operation PhysT (qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("rx")
    operation PhysRx (theta : Double, qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("rz")
    operation PhysRz (theta : Double, qb : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("cnot")
    operation PhysCNOT (control : Qubit, target : Qubit) : Unit 
    {
        body intrinsic;
    }

    @TargetInstruction("mz")
    operation PhysM (qb : Qubit) : Result
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
    {
        PhysS(qb);  
	}

    @Inline()
    operation T(qb : Qubit) : Unit
    {
        PhysT(qb);  
	}

    @Inline()
    operation M(qb : Qubit) : Result
    {
        return PhysM(qb);  
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
    is Adj {
        body  (...)
        {
            PhysRz(theta, qb);  
		}
        adjoint (...)
        {
            PhysRz(-theta, qb);  
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
}
