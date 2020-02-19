/// This namespace contains additional definitions used by the test cases for access modifiers.
namespace Microsoft.Quantum.Testing.AccessModifiers {
    private newtype T1 = Unit;

    internal newtype T2 = Unit;

    private function F1 () : Unit {}

    internal function F2 () : Unit {}
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.C {
    private function CF1 () : Unit {}

    internal function CF2 () : Unit {}

    private newtype CT1 = Unit;

    internal newtype CT2 = Unit;
}
