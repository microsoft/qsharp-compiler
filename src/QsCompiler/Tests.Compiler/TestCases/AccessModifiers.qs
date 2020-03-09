// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for access modifiers.
namespace Microsoft.Quantum.Testing.AccessModifiers {
    open Microsoft.Quantum.Testing.AccessModifiers.A;
    open Microsoft.Quantum.Testing.AccessModifiers.B as B;
    open Microsoft.Quantum.Testing.AccessModifiers.C;

    // Redefine inaccessible references (see TestTargets\Libraries\Library1\AccessModifiers.qs)

    internal newtype InternalType = Unit;

    internal function InternalFunction () : Unit {}

    // Callables

    function CallableUseOK () : Unit {
        InternalFunction();
        InternalFunctionA();
        B.InternalFunctionB();
    }

    // Types

    function TypeUseOK () : Unit {
        let it = InternalType();
        let its = new InternalType[1];
        let ita = InternalTypeA();
        let itas = new InternalTypeA[1];
        let itb = B.InternalTypeB();
        let itbs = new B.InternalTypeB[1];
    }

    // Callable signatures

    function CallableLeaksInternalTypeIn1 (x : InternalType) : Unit {}

    function CallableLeaksInternalTypeIn2 (x : (Int, InternalType)) : Unit {}

    function CallableLeaksInternalTypeIn3 (x : (Int, (InternalType, Bool))) : Unit {}

    function CallableLeaksInternalTypeOut1 () : InternalType {
        return InternalType();
    }

    function CallableLeaksInternalTypeOut2 () : (Int, InternalType) {
        return (0, InternalType());
    }

    function CallableLeaksInternalTypeOut3 () : (Int, (InternalType, Bool)) {
        return (0, (InternalType(), false));
    }

    internal function InternalCallableInternalTypeOK (x : InternalType) : InternalType {
        return InternalType();
    }

    // Underlying types

    newtype PublicTypeLeaksInternalType1 = InternalType;

    newtype PublicTypeLeaksInternalType2 = (Int, InternalType);

    newtype PublicTypeLeaksInternalType3 = (Int, (InternalType, Bool));

    internal newtype InternalTypeInternalTypeOK = InternalType;

    // References

    function CallableReferenceInternalInaccessible () : Unit {
        InternalFunctionC();
    }

    function TypeReferenceInternalInaccessible () : Unit {
        let itcs = new InternalTypeC[1];
    }

    function TypeConstructorReferenceInternalInaccessible () : Unit {
        let itc = InternalTypeC();
    }
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.A {
    internal function InternalFunctionA () : Unit {}

    internal newtype InternalTypeA = Unit;
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.B {
    internal function InternalFunctionB () : Unit {}

    internal newtype InternalTypeB = Unit;
}
