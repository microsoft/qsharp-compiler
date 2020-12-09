// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Targeting;

    @TargetInstruction("swap")
    operation _SWAP(q1 : Qubit, q2 : Qubit) : Unit {
        body intrinsic;
	}

    @TargetInstruction("choose")
    operation _Choose(q1 : Qubit, (q2 : Qubit, q3 : Qubit)) : Unit {
        body intrinsic;
	}

    operation Apply(op : (Qubit => Unit)) : Unit {
        using (q = Qubit()) {
            H(q);
            op(q);
        }
    }

    @EntryPoint()
    operation TestOpArgument () : Unit {
        using (q = Qubit()) {
            Apply(CNOT(_, q)); 
            Apply(_SWAP(_, q));
            Apply(_Choose(q, (_, q))); 
            Apply(_Choose(_,(q,q)));
        }
    }
}
