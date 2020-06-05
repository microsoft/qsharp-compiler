// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// Test cases for verification of execution target runtime capabilities.
namespace Microsoft.Quantum.Testing.CapabilityVerification {
    internal operation X(q : Qubit) : Unit {
        body intrinsic;
    }

    internal operation M(q : Qubit) : Result {
        body intrinsic;
    }

    function ResultAsBool(result : Result) : Bool {
        return result == Zero ? false | true;
    }

    operation ResultAsBoolOp(result : Result) : Bool {
        return result == Zero ? false | true;
    }

    operation ResultAsBoolOpReturnIf(result : Result) : Bool {
        if (result == Zero) {
            return false;
        } else {
            return true;
        }
    }

    operation ResultAsBoolOpSetIf(result : Result) : Bool {
        mutable b = false;
        if (result == One) {
            set b = true;
        }
        return b;
    }

    operation EmptyIf(result : Result) : Unit {
        if (result == Zero) { }
    }

    operation Reset(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }
}
