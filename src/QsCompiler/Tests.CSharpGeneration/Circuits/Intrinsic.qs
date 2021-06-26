// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Some Intrinsic stubs
// Notice that CNOT is defined in CodegenTests.qs for testing.
namespace Microsoft.Quantum.Intrinsic {
    
    newtype Qubits = Qubit[];
    
    
    operation H (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }
    
    
    operation S (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }
    
    
    operation X (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }
    
    
    operation Z (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }
    
    
    operation R (d : Double, q : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
    }
    
    
    operation M (q1 : Qubit) : Result {
        body intrinsic;
    }
    
}


