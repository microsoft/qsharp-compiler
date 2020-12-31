// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    operation SelfAdjointIntrinsic() : Unit
    is Adj + Ctl {
        body intrinsic;
        adjoint self;
    }

    operation SelfAdjointOp() : Unit
    is Adj + Ctl {
        body (...) { }
        adjoint self;
    }

    @EntryPoint()
    operation TestDeclarations () : Unit {
        using (q = Qubit()) {

            Adjoint SelfAdjointIntrinsic();
            Controlled Adjoint SelfAdjointIntrinsic([q], ());

            Adjoint SelfAdjointOp(); 
            Controlled Adjoint SelfAdjointOp([q], ());       
        }
    }
}
