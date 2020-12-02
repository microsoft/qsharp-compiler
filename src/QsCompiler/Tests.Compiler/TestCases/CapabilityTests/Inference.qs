namespace Microsoft.Quantum.Testing.Capability {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Targeting;

    // Inferred capabilities can be overridden or given explicitly.

    @RequiresCapability("BasicMeasurementFeedback", "Test case.")
    operation OverrideBqfToBmf(q : Qubit) : Unit {
        X(q);
    }

    @RequiresCapability("FullComputation", "Test case.")
    operation OverrideBmfToFull(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("BasicQuantumFunctionality", "Test case.")
    operation OverrideBmfToBqf(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    @RequiresCapability("BasicMeasurementFeedback", "Test case.")
    operation OverrideFullToBmf(q : Qubit) : Bool {
        return M(q) == One ? true | false;
    }

    @RequiresCapability("BasicMeasurementFeedback", "Test case.")
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

    @RequiresCapability("FullComputation", "Test case.")
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

    @RequiresCapability("BasicMeasurementFeedback", "Test case.")
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

    // BasicMeasurementFeedback repeat-until loop

    operation BmfRepeatUntil(q : Qubit) : Unit {
        repeat {
            X(q);
        }
        until (M(q) == One);
    }

    operation BmfRepeatUntilFixup(q : Qubit) : Unit {
        repeat {
            X(q);
        }
        until (M(q) == One)
        fixup {
            Reset(q);
        }
    }

    // FullComputation repeat-until loop

    operation FullCRepeatUntil(q : Qubit) : Unit {
        mutable r = Zero;
        repeat {
            X(q);
            set r = M(q);
        }
        until (r == One);
    }

    operation FullCRepeatUntilFixup(q : Qubit) : Unit {
        mutable r = M(q);
        repeat {

        }
        until (r == One)
        fixup {
            X(q);
            set r = M(q);
        }
    }

    // BasicQuantumFunctionality repeat-until loop

    operation BqfRepeatUntilFixup(q : Qubit) : Unit {
        mutable i = 0;
        repeat {
            X(q);
        }
        until (i <= 0)
        fixup {
            set i = i + 1;
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
