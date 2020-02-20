// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for access modifiers.
namespace Microsoft.Quantum.Testing.AccessModifiers {
    open Microsoft.Quantum.Testing.AccessModifiers.A;
    open Microsoft.Quantum.Testing.AccessModifiers.B as B;
    open Microsoft.Quantum.Testing.AccessModifiers.C;
    
    // Redefine inaccessible references (see TestTargets\Libraries\Library1\AccessModifiers.qs)

    private newtype PrivateType = Unit;

    internal newtype InternalType = Unit;

    private function PrivateFunction () : Unit {}

    internal function InternalFunction () : Unit {}

    // Callables

    function CallableUseOK () : Unit {
        PrivateFunction();
        InternalFunction();
        InternalFunctionA();
        B.InternalFunctionB();
    }

    function CallableUnqualifiedUsePrivateInaccessible () : Unit {
        PrivateFunctionA();
    }

    function CallableQualifiedUsePrivateInaccessible () : Unit {
        B.PrivateFunctionB();
    }

    // Types

    function TypeUseOK () : Unit {
        let pt = PrivateType();
        let pts = new PrivateType[1];
        let it = InternalType();
        let its = new InternalType[1];
        let ita = InternalTypeA();
        let itas = new InternalTypeA[1];
        let itb = B.InternalTypeB();
        let itbs = new B.InternalTypeB[1];
    }

    function TypeUnqualifiedUsePrivateInaccessible () : Unit {
        let ptas = new PrivateTypeA[1];
    }

    function TypeConstructorUnqualifiedUsePrivateInaccessible () : Unit {
        let pta = PrivateTypeA();
    }

    function TypeQualifiedUsePrivateInaccessible () : Unit {
        let ptbs = new B.PrivateTypeB[1];
    }

    function TypeConstructorQualifiedUsePrivateInaccessible () : Unit {
        let ptb = B.PrivateTypeB();
    }

    // Callable signatures

    function PublicCallableLeaksPrivateTypeIn1 (x : PrivateType) : Unit {}
    
    function PublicCallableLeaksPrivateTypeIn2 (x : (Int, (PrivateType, Bool))) : Unit {}

    function PublicCallableLeaksPrivateTypeOut1 () : PrivateType {
        return PrivateType();
    }

    function PublicCallableLeaksPrivateTypeOut2 () : (Double, ((Result, PrivateType), Bool)) {
        return (1.0, ((Zero, PrivateType()), true));
    }

    internal function InternalCallableLeaksPrivateTypeIn (x : PrivateType) : Unit {}

    internal function InternalCallableLeaksPrivateTypeOut () : PrivateType {
        return PrivateType();
    }

    private function CallablePrivateTypeOK (x : PrivateType) : PrivateType {
        return PrivateType();
    }

    function CallableLeaksInternalTypeIn (x : InternalType) : Unit {}

    function CallableLeaksInternalTypeOut () : InternalType {
        return InternalType();
    }

    internal function InternalCallableInternalTypeOK (x : InternalType) : InternalType {
        return InternalType();
    }

    private function PrivateCallableInternalTypeOK (x : PrivateType) : PrivateType {
        return PrivateType();
    }

    // Underlying types

    newtype PublicTypeLeaksPrivateType1 = PrivateType;

    newtype PublicTypeLeaksPrivateType2 = (Int, PrivateType);

    newtype PublicTypeLeaksPrivateType3 = (Int, (PrivateType, Bool));

    internal newtype InternalTypeLeaksPrivateType = PrivateType;

    private newtype PrivateTypePrivateTypeOK = PrivateType;

    newtype PublicTypeLeaksInternalType = InternalType;

    internal newtype InternalTypeInternalTypeOK = InternalType;

    private newtype PrivateTypeInternalTypeOK = InternalType;

    // References

    function CallableReferencePrivateInaccessible () : Unit {
        PrivateFunctionC();
    }

    function CallableReferenceInternalInaccessible () : Unit {
        InternalFunctionC();
    }

    function TypeReferencePrivateInaccessible () : Unit {
        let ptcs = new PrivateTypeC[1];
    }

    function TypeConstructorReferencePrivateInaccessible () : Unit {
        let ptc = PrivateTypeC();
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
    private function PrivateFunctionA () : Unit {}

    internal function InternalFunctionA () : Unit {}

    private newtype PrivateTypeA = Unit;

    internal newtype InternalTypeA = Unit;
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.B {
    private function PrivateFunctionB () : Unit {}

    internal function InternalFunctionB () : Unit {}

    private newtype PrivateTypeB = Unit;

    internal newtype InternalTypeB = Unit;
}
