// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for access modifiers.
namespace Microsoft.Quantum.Testing.AccessModifiers {
    open Microsoft.Quantum.Testing.AccessModifiers.A;
    open Microsoft.Quantum.Testing.AccessModifiers.B as B;
    open Microsoft.Quantum.Testing.AccessModifiers.C;
    
    // Redefine inaccessible references (see TestTargets\Libraries\Library1\AccessModifiers.qs)

    private newtype T1 = Unit;

    internal newtype T2 = Unit;

    private function F1 () : Unit {}

    internal function F2 () : Unit {}

    // Callables

    function CallableUseOK () : Unit {
        F1();
        F2();
        AF2();
        B.BF2();
    }

    function CallableUnqualifiedUsePrivateInaccessible () : Unit {
        AF1();
    }

    function CallableQualifiedUsePrivateInaccessible () : Unit {
        B.BF1();
    }

    // Types

    function TypeUseOK () : Unit {
        let t1 = T1();
        let t1s = new T1[1];
        let t2 = T2();
        let t2s = new T2[1];
        let at2 = AT2();
        let at2s = new AT2[1];
        let bt2 = B.BT2();
        let bt2s = new B.BT2[1];
    }

    function TypeUnqualifiedUsePrivateInaccessible () : Unit {
        let at1s = new AT1[1];
    }

    function TypeConstructorUnqualifiedUsePrivateInaccessible () : Unit {
        let at1 = AT1();
    }

    function TypeQualifiedUsePrivateInaccessible () : Unit {
        let bt1s = new B.BT1[1];
    }

    function TypeConstructorQualifiedUsePrivateInaccessible () : Unit {
        let bt1 = B.BT1();
    }

    // Callable signatures

    function PublicCallableLeaksPrivateTypeIn1 (x : T1) : Unit {}
    
    function PublicCallableLeaksPrivateTypeIn2 (x : (Int, (T1, Bool))) : Unit {}

    function PublicCallableLeaksPrivateTypeOut1 () : T1 {
        return T1();
    }

    function PublicCallableLeaksPrivateTypeOut2 () : (Double, ((Result, T1), Bool)) {
        return (1.0, ((Zero, T1()), true));
    }

    internal function InternalCallableLeaksPrivateTypeIn (x : T1) : Unit {}

    internal function InternalCallableLeaksPrivateTypeOut () : T1 {
        return T1();
    }

    private function CallablePrivateTypeOK (x : T1) : T1 {
        return T1();
    }

    function CallableLeaksInternalTypeIn (x : T2) : Unit {}

    function CallableLeaksInternalTypeOut () : T2 {
        return T2();
    }

    internal function InternalCallableInternalTypeOK (x : T2) : T2 {
        return T2();
    }

    private function PrivateCallableInternalTypeOK (x : T1) : T1 {
        return T1();
    }

    // Underlying types

    newtype PublicTypeLeaksPrivateType1 = T1;

    newtype PublicTypeLeaksPrivateType2 = (Int, T1);

    newtype PublicTypeLeaksPrivateType3 = (Int, (T1, Bool));

    internal newtype InternalTypeLeaksPrivateType = T1;

    private newtype PrivateTypePrivateTypeOK = T1;

    newtype PublicTypeLeaksInternalType = T2;

    internal newtype InternalTypeInternalTypeOK = T2;

    private newtype PrivateTypeInternalTypeOK = T2;

    // References

    function CallableReferencePrivateInaccessible () : Unit {
        CF1();
	}

    function CallableReferenceInternalInaccessible () : Unit {
        CF2();
	}

    function TypeReferencePrivateInaccessible () : Unit {
        let ct1s = new CT1[1];
	}

    function TypeConstructorReferencePrivateInaccessible () : Unit {
        let ct1 = CT1();
	}

    function TypeReferenceInternalInaccessible () : Unit {
        let ct2s = new CT2[1];
	}

    function TypeConstructorReferenceInternalInaccessible () : Unit {
        let ct2 = CT2();
	}
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.A {
    private function AF1 () : Unit {}

    internal function AF2 () : Unit {}

    private newtype AT1 = Unit;

    internal newtype AT2 = Unit;
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.B {
    private function BF1 () : Unit {}

    internal function BF2 () : Unit {}

    private newtype BT1 = Unit;

    internal newtype BT2 = Unit;
}
