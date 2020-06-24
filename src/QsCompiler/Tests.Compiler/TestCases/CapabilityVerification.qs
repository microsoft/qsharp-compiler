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

    internal operation NoOp() : Unit { }

    function ResultAsBool(result : Result) : Bool {
        return result == Zero ? false | true;
    }

    function ResultAsBoolNeq(result : Result) : Bool {
        return result != One ? false | true;
    }

    operation ResultAsBoolOp(result : Result) : Bool {
        return result == Zero ? false | true;
    }

    function ResultAsBoolNeqOp(result : Result) : Bool {
        return result != One ? false | true;
    }

    operation ResultAsBoolOpReturnIf(result : Result) : Bool {
        if (result == Zero) {
            return false;
        } else {
            return true;
        }
    }

    operation ResultAsBoolNeqOpReturnIf(result : Result) : Bool {
        if (result != One) {
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

    operation ResultAsBoolNeqOpSetIf(result : Result) : Bool {
        mutable b = false;
        if (result != Zero) {
            set b = true;
        }
        return b;
    }

    operation ResultAsBoolOpElseSet(result : Result) : Bool {
        mutable b = false;
        if (result == Zero) {
            NoOp();
        } else {
            set b = true;
        }
        return b;
    }

    operation ElifSet(result : Result, flag : Bool) : Bool {
        mutable b = false;
        if (flag) {
            set b = true;
        } elif (result != Zero) {
            set b = true;
        }
        return b;
    }

    operation ElifElifSet(result : Result, flag : Bool) : Bool {
        mutable b = false;
        if (flag) {
            set b = true;
        } elif (flag) {
            set b = true;
        } elif (result != Zero) {
            set b = true;
        } else {
            NoOp();
        }
        return b;
    }

    operation ElifElseSet(result : Result, flag : Bool) : Bool {
        mutable b = false;
        if (flag) {
            set b = true;
        } elif (result == Zero) {
            NoOp();
        } else {
            set b = true;
        }
        return b;
    }

    function EmptyIf(result : Result) : Unit {
        if (result == Zero) { }
    }

    function EmptyIfNeq(result : Result) : Unit {
        if (result != Zero) { }
    }

    operation EmptyIfOp(result : Result) : Unit {
        if (result == Zero) { }
    }

    operation EmptyIfNeqOp(result : Result) : Unit {
        if (result != Zero) { }
    }

    operation Reset(q : Qubit) : Unit {
        if (M(q) == One) {
            X(q);
        }
    }

    operation ResetNeq(q : Qubit) : Unit {
        if (M(q) != Zero) {
            X(q);
        }
    }
}
