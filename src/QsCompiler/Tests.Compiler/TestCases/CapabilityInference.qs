namespace Microsoft.Quantum.Testing.Capability {
    open Microsoft.Quantum.Intrinsic;

    // Inferred capabilities can be overridden or given explicitly.

    @Capability("QPRGen1")
    operation OverrideGen0ToGen1(q : Qubit) : Unit {
        X(q);
    }

    @Capability("Unknown")
    operation OverrideGen1ToUnknown(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @Capability("QPRGen0")
    operation OverrideGen1ToGen0(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @Capability("QPRGen1")
    operation OverrideUnknownToGen1(q : Qubit) : Bool {
        return M(q) == One ? true | false;
    }

    @Capability("QPRGen1")
    operation ExplicitGen1(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }
}

namespace Microsoft.Quantum.Core {
    @Attribute()
    newtype Attribute = Unit;

    @Attribute()
    newtype Capability = String;
}
