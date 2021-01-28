// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
   
namespace Microsoft.Quantum.Testing.QIR
{
    operation ArbitraryAllocation (max : Int, q : Qubit) : Unit {
        using ((a, (b, c), d) = (Qubit(), (Qubit[max], Qubit()), Qubit[2])) {

            let x = b[1];
            borrowing (z = Qubit()) {

                let y = b[0..2..max];
                if (Length(y) == max) {
                    return ();
                }
            }
        }
    }

    @EntryPoint()
    operation TestUsing() : Unit
    {
        using (q = Qubit())
        {
            ArbitraryAllocation(3, q);
        }
    }

}
