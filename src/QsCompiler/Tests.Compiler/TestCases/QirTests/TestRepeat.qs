// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    newtype Energy = (Abs : Double, Name : String);

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

            let name = "energy";
            mutable res = Energy(0.0, "");
            set res w/= Name <- name;

            set n += 1;
        }
        until (M(q) == Zero)
        fixup
        {
            if (n > 100)
            {
                fail "Too many iterations";
            }

            set res w/= Name <- "";
        }
        return n;
    }
}
