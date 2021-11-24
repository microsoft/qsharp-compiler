// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    newtype Energy = (Abs : Double, Name : String);

    operation TestRepeat2 (q : Qubit) : Result[] {

        mutable (iter, res) = (1, []);
        repeat {
            H(q);
            set res += [M(q)];
        }
        until (iter > 3)
        fixup {
            set iter *= 2;
        }

        return res;
    }

    operation TestRepeat1 (q : Qubit) : Int
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

    @EntryPoint()
    operation Main() : Unit {
        use q = Qubit();
        let _ = TestRepeat1(q);
        let _ = TestRepeat2(q);
    }
}
