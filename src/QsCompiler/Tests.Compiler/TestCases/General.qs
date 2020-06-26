// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains declarations for use in unit testing
namespace Microsoft.Quantum.Testing.General {

    newtype BigEndian = Qubit[];

    operation DoNothing () : Unit {}

    function Function () : Unit {
        body intrinsic;
    }

    function GenericFunction<'T> (a : 'T) : Unit {
        body intrinsic;
    }

    operation Operation (q : Qubit) : Unit {
        body intrinsic;
    }

    operation GenericOperation<'T> (a : 'T) : Unit {
        body intrinsic;
    }

    operation Adjointable (q : Qubit) : Unit {
        body intrinsic;
        adjoint auto;
    }

    operation GenericAdjointable<'T> (q : 'T) : Unit {
        body intrinsic;
        adjoint auto;
    }

    operation Controllable (q : Qubit) : Unit {
        body intrinsic;
        controlled auto;
    }

    operation GenericControllable<'T> (a : 'T) : Unit {
        body intrinsic;
        controlled auto;
    }

    operation Unitary (q : Qubit) : Unit {
        body intrinsic;
        adjoint auto;
        controlled auto;
        controlled adjoint auto;
    }

    operation GenericUnitary<'T> (a : 'T) : Unit {
        body intrinsic;
        adjoint auto;
        controlled auto;
        controlled adjoint auto;
    }

    operation M (q : Qubit) : Result {
        body intrinsic;
    }

    operation CoinFlip() : Bool {
        body intrinsic;
    }
}

