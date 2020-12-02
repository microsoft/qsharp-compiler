// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation TestPartials () : Bool
    {
        let rotate = Rz(0.25, _);
        let unrotate = Adjoint rotate;

        for (i in 0..100)
        {
            using (qb = Qubit())
            {
                rotate(qb);
                unrotate(qb);
                if (M(qb) != Zero)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
