// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    operation TestRepeat (q : Qubit) : Int
    {
        mutable n = 0;
        repeat
        {
            within
            {
                T(q);
            }
            apply
            {
                X(q);
            }
            H(q);
        }
        until (M(q) == Zero)
        fixup
        {
            set n = n + 1;
            if (n > 100)
            {
                fail "Too many iterations";
            }
        }
        return n;
    }
}
