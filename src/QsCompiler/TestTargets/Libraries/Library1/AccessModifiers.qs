/// This file contains redefinitions of types and callables declared in Tests.Compiler\TestCases\AccessModifiers.qs. It
/// is used as an assembly reference to test support for re-using names of inaccessible declarations in references.
namespace Microsoft.Quantum.Testing.AccessModifiers {
    internal newtype InternalType = Unit;

    internal function InternalFunction () : Unit {}
}

/// This namespace contains additional definitions of types and callables meant to be used by the
/// Microsoft.Quantum.Testing.AccessModifiers namespace.
namespace Microsoft.Quantum.Testing.AccessModifiers.C {
    internal newtype InternalTypeC = Unit;

    internal function InternalFunctionC () : Unit {}
}
