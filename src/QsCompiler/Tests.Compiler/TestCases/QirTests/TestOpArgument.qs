// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Targeting;

    @TargetInstruction("message")
    function Message() : String {
        body intrinsic;
    }

    operation Diagnose() : Unit is Adj + Ctl {
        body intrinsic;
    }

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

    function InvokeAndIgnore<'T>(op : (Unit -> 'T)) : Unit {
        let _ = op();
    }

    function GetNestedTuple() : (Int, (String, Double)) {
        return (1, ("", 2.));
    }

    @EntryPoint()
    operation TestOpArgument () : String {
        using (qs = (Qubit(), Qubit())) {
            let (q1, q2) = qs;
            let op = _Choose(_, (q1, _));

            CNOT(qs);
            _SWAP(qs);
            Apply(CNOT(_, q1)); 
            Apply(_SWAP(_, q1));
            Apply(_Choose(q1, (_, q2))); 
            Apply(_Choose(_,(q1,q2)));
            Apply(_Choose(_,qs));
            op(qs);
        }
        InvokeAndIgnore(GetNestedTuple);
        Diagnose();
        return Message();
    }
}
