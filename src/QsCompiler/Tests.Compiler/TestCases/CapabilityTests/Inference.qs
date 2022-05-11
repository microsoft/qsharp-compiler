namespace Microsoft.Quantum.Testing.Capability {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Targeting;

    // Inferred capabilities can be overridden or given explicitly.

    @RequiresCapability("Controlled", "Empty", "Test case.")
    operation OverrideBqfToBmf(q : Qubit) : Unit {
        X(q);
    }

    @RequiresCapability("Transparent", "Empty", "Test case.")
    operation OverrideBmfToFull(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("Opaque", "Empty", "Test case.")
    operation OverrideBmfToBqf(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("Controlled", "Empty", "Test case.")
    operation OverrideFullToBmf(q : Qubit) : Bool {
        return M(q) == One ? true | false;
    }

    @RequiresCapability("Controlled", "Empty", "Test case.")
    operation ExplicitBmf(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // BasicMeasurementFeedback (1 dependency)

    operation CallBmfA(q : Qubit) : Unit {
        CallBmfB(q);
    }

    operation CallBmfB(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // Call BasicMeasurementFeedback and FullComputation (2 dependencies, 1 layer)

    operation CallBmfFullA(q : Qubit) : Unit {
        let r = CallBmfFullB(q);
        let b = CallBmfFullC(r);
    }

    operation CallBmfFullB(q : Qubit) : Result {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r;
    }

    operation CallBmfFullC(r : Result) : Bool {
        return r == One ? true | false;
    }

    // FullComputation (2 dependencies)

    operation CallFullA(q : Qubit) : Bool {
        return CallFullB(q);
    }

    operation CallFullB(q : Qubit) : Bool {
        return CallFullC(q) == One ? true | false;
    }

    operation CallFullC(q : Qubit) : Result {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r;
    }

    // Override BasicMeasurementFeedback to FullComputation (2 dependencies)

    operation CallFullOverrideA(q : Qubit) : Unit {
        CallFullOverrideB(q);
    }

    @RequiresCapability("Transparent", "Empty", "Test case.")
    operation CallFullOverrideB(q : Qubit) : Unit {
        CallFullOverrideC(q);
    }

    operation CallFullOverrideC(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    // Override FullComputation to BasicMeasurementFeedback (2 dependencies)

    operation CallBmfOverrideA(q : Qubit) : Bool {
        return CallBmfOverrideB(q);
    }

    @RequiresCapability("Controlled", "Empty", "Test case.")
    operation CallBmfOverrideB(q : Qubit) : Bool {
        return CallBmfOverrideC(q);
    }

    operation CallBmfOverrideC(q : Qubit) : Bool {
        let r = M(q);
        if (r == One) {
            X(q);
        }
        return r == One ? true | false;
    }

    // BasicMeasurementFeedback direct recursion

    operation BmfRecursion(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
            BmfRecursion(q);
        }
    }

    // BasicMeasurementFeedback period-3 recursion

    operation BmfRecursion3A(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
            BmfRecursion3B(q);
        }
    }

    operation BmfRecursion3B(q : Qubit) : Unit {
        BmfRecursion3C(q);
    }

    operation BmfRecursion3C(q : Qubit) : Unit {
        BmfRecursion3A(q);
    }

    // BasicMeasurementFeedback reference without call

    operation ReferenceBmfA() : (Qubit => Unit) {
        return ReferenceBmfB;
    }

    operation ReferenceBmfB(q : Qubit) : Unit {
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
    newtype RequiresCapability = (ResultOpacity : String, Classical : String, Reason : String);
}
