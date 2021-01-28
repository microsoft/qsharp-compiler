// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype LittleEndian = Qubit[];

    operation Empty (qs : Qubit[]) : Unit is Adj + Ctl {
    }

    operation DoNothing (qs : Qubit[]) : Unit is Adj + Ctl {
        let dummy = 1;
    }

    function ReturnDoNothing (dummy : Int) : (Qubit[] => Unit is Adj + Ctl)
    {
        return DoNothing;
    }

    operation X (q : Qubit) : Unit is Adj + Ctl {
        body intrinsic;
    }

    operation CNOT (control : Qubit, target : Qubit) : Unit
    is Adj + Ctl {

        body (...) {
            Controlled X([control], target);
        }
        
        adjoint self;
    }

    function TakesSingleTupleArg(gen : (Int, (Int -> (Qubit[] => Unit is Adj + Ctl)))) 
    : Unit {
    }

    @EntryPoint()
    operation TestOperationCalls() : Unit {

        let doNothing = ReturnDoNothing(1);
        using (aux = Qubit())
        {
            CNOT(aux, aux);
            Empty([aux]);
            doNothing([aux]);
            Controlled DoNothing([aux], [aux]);
        }
        TakesSingleTupleArg(2, ReturnDoNothing);
    }
}
