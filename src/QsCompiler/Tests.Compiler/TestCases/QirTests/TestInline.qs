// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
  
namespace Microsoft.Quantum.Instructions {
 
    operation K (theta : Double, qb : Qubit) : Unit {
        body intrinsic;
    }
}
 
namespace Microsoft.Quantum.Intrinsic {
 
    open Microsoft.Quantum.Targeting;
    open Microsoft.Quantum.Instructions as Phys;
    
    @Inline()
    operation K(theta : Double, qb : Qubit) : Unit
    is Adj {
        body  (...)
        {
            Phys.K(theta, qb);  
        }
        adjoint (...)
        {
            Phys.K(-theta, qb);
        }
    }
}
 
namespace Microsoft.Quantum.Testing.QIR
{
    open Microsoft.Quantum.Intrinsic;
 
    internal function UpdatedValues(res : Result, (x : Double, y : Double)) : (Double, Double) {
        return res == Zero ? (x - 0.5, y) | (x, y + 0.5);
    }

    @EntryPoint()
    operation TestInline() : (Double, Double) {

        mutable (x, y) = (0., 0.);
        using (q = Qubit()) {

            K(0.3, q);
            set (x, y) = UpdatedValues(M(q), (x, y));
        }

        return (x, y);
    }
}
