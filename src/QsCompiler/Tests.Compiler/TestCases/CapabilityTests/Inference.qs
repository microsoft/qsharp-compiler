namespace Microsoft.Quantum.Testing.Capability {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Targeting;

    // Inferred capabilities can be overridden or given explicitly.

    @RequiresCapability("QPRGen1", "Test case.")
    operation OverrideGen0ToGen1(q : Qubit) : Unit {
        X(q);
    }

    @RequiresCapability("Unknown", "Test case.")
    operation OverrideGen1ToUnknown(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("QPRGen0", "Test case.")
    operation OverrideGen1ToGen0(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("QPRGen1", "Test case.")
    operation OverrideUnknownToGen1(q : Qubit) : Bool {
        return M(q) == One ? true | false;
    }

    @RequiresCapability("QPRGen1", "Test case.")
    operation ExplicitGen1(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // QPRGen1 (1 dependency)

    operation CallGen1A(q : Qubit) : Unit {
        CallGen1B(q);
    }

    operation CallGen1B(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // Call QPRGen1 and Unknown (2 dependencies, 1 layer)

    operation CallGen1UnknownA(q : Qubit) : Unit {
        let r = CallGen1UnknownB(q);
        let b = CallGen1UnknownC(r);
    }

    operation CallGen1UnknownB(q : Qubit) : Result {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r;
    }

    operation CallGen1UnknownC(r : Result) : Bool {
        return r == One ? true | false;
    }

    // Unknown (2 dependencies)

    operation CallUnknownA(q : Qubit) : Bool {
        return CallUnknownB(q);
    }

    operation CallUnknownB(q : Qubit) : Bool {
        return CallUnknownC(q) == One ? true | false;
    }

    operation CallUnknownC(q : Qubit) : Result {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r;
    }

    // Override QPRGen1 to Unknown (2 dependencies)

    operation CallUnknownOverrideA(q : Qubit) : Unit {
        CallUnknownOverrideB(q);
    }

    @RequiresCapability("Unknown", "Test case.")
    operation CallUnknownOverrideB(q : Qubit) : Unit {
        CallUnknownOverrideC(q);
    }

    operation CallUnknownOverrideC(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // Override Unknown to QPRGen1 (2 dependencies)

    operation CallQPRGen1OverrideA(q : Qubit) : Bool {
        return CallQPRGen1OverrideB(q);
    }

    @RequiresCapability("QPRGen1", "Test case.")
    operation CallQPRGen1OverrideB(q : Qubit) : Bool {
        return CallQPRGen1OverrideC(q);
    }

    operation CallQPRGen1OverrideC(q : Qubit) : Bool {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r == One ? true | false;
    }

    // QPRGen1 direct recursion

    operation Gen1Recursion(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
            Gen1Recursion(q);
        }
    }

    // QPRGen1 period-3 recursion
    
    operation Gen1Recursion3A(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
            Gen1Recursion3B(q);
        }
    }

    operation Gen1Recursion3B(q : Qubit) : Unit {
        Gen1Recursion3C(q);
    }

    operation Gen1Recursion3C(q : Qubit) : Unit {
        Gen1Recursion3A(q);
    }

    // QPRGen1 reference without call

    operation ReferenceGen1A() : (Qubit => Unit) {
        return ReferenceGen1B;
    }

    operation ReferenceGen1B(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }
}

namespace Microsoft.Quantum.Core {
    @Attribute()
    newtype Attribute = Unit;
}

namespace Microsoft.Quantum.Targeting {
    @Attribute()
    newtype RequiresCapability = (Level : String, Reason : String);
}
