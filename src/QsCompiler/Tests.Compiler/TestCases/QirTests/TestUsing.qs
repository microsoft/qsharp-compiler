// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    operation TestUsing () : Int
    {
        using ((a, (b, c), d) = (Qubit(), (Qubit[3], Qubit()), Qubit[2]))
        {
            let x = b[1];
            using (z = Qubit())
            {
                let y = b[0..2..3];
                if (Length(y) == 3)
                {
                    return 5;
                }
            }
        }
        return 4;
    }
}
