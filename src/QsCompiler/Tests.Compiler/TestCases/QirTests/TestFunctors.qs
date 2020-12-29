// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// For the test to pass implement K to be the same as X. We need it for the test, because the standard bridge doesn't
// support multi-controlled X.
namespace Microsoft.Quantum.Intrinsic
{
    open Microsoft.Quantum.Instructions;

    @Inline()
    operation K(q : Qubit) : Unit
    is Adj+Ctl {
        body intrinsic;
        adjoint self;
        controlled intrinsic;
    }
}

namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;

    operation Qop(q : Qubit, n : Int) : Unit
    is Adj+Ctl {
        body (...)
        {
            if (n%2 == 1) { K(q); }
        }
        adjoint (...) // self, but have to define explicitly due to https://github.com/microsoft/qsharp-compiler/issues/781
        {
            if (n%2 == 1) { K(q); }
        }
        controlled (ctrls, ...)
        {
            if (n%2 == 1) { Controlled K(ctrls, q); }
        }
    }

    @EntryPoint()
    operation TestControlled () : Int
    {
        let qop = Qop(_, 1);
        let adj_qop = Adjoint qop;
        let ctl_qop = Controlled qop;
        let adj_ctl_qop = Adjoint Controlled qop;
        let ctl_ctl_qop = Controlled ctl_qop;

        mutable error_code = 0;
        using ((q1, q2, q3) = (Qubit(), Qubit(), Qubit()))
        {
            qop(q1);
            if (M(q1) != One) { set error_code = 1; }
            else
            {
                adj_qop(q2);
                if (M(q2) != One) { set error_code = 2; }
                else{
                    ctl_qop([q1], q3);
                    if (M(q3) != One) { set error_code = 3; }
                    else
                    {
                        adj_ctl_qop([q2], q3);
                        if (M(q3) != Zero) { set error_code = 4; }
                        else
                        {
                            ctl_ctl_qop([q1], ([q2], q3));
                            if (M(q3) != One) { set error_code = 5; }
                            else
                            {
                                Controlled qop([q1, q2], q3);
                                if (M(q3) != Zero) { set error_code = 6; }
                                else
                                {
                                    using (q4 = Qubit())
                                    {
                                        Adjoint qop(q3);
                                        Adjoint Controlled ctl_ctl_qop([q1], ([q2], ([q3], q4)));
                                        if (M(q4) != One) { set error_code = 7; }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return error_code;
    }
}
