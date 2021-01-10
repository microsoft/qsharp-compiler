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
 
namespace Microsoft.Quantum.Testing.Tracer
{
    open Microsoft.Quantum.Intrinsic;
 
    @EntryPoint()
    operation TestInline() : Unit
    {
        using (q = Qubit())
        {
            K(0.3, q);
        }
    }
}
