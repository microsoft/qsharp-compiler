// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This test extends the basic target with a K gate that implements Controlled.
// THis isn't a real gate, it's just used for testing purposes.
namespace Microsoft.Quantum.Instructions
{
    @Intrinsic("k")
    operation PhysK (qb : Qubit, n : Int) : Unit 
    {
        body intrinsic;
    }

    @Intrinsic("ck")
    operation PhysCtrlK (ctrls : Qubit[], qb : Qubit, n : Int) : Unit 
    {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Intrinsic
{
    open Microsoft.Quantum.Instructions;

    @Inline()
    operation K(qb : Qubit, n : Int) : Unit
    is Adj+Ctl {
        body (...)
        {
            PhysK(qb, n);  
		}
        adjoint self;
        controlled (ctrls, ...)
        {
            PhysCtrlK(ctrls, qb, n);
        }
	}
}

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation TestControlled () : Bool
    {
        let k2 = K(_, 2);
        let ck2 = Controlled k2;
        let ck1 = K(_, _);

        for (i in 0..100)
        {
            using ((ctrls, qb) = (Qubit[2], Qubit()))
            {
                ck2(ctrls, qb);
                using (moreCtrls = Qubit[3])
                {
                    Controlled ck2(moreCtrls, (ctrls, qb));
                    Controlled ck1(ctrls, (qb, 1));
                }
                if (M(qb) != Zero)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
