// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// Test cases for verification of execution target runtime capabilities.
namespace Microsoft.Quantum.Testing.Capability {
    open Microsoft.Quantum.Intrinsic;

    operation NoOp() : Unit { }

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

    operation ResultAsBoolOpReturnIfNested(result : Result) : Bool {
        if (result == Zero) {
            let x = 5;
            if (x == 5) {
                return false;
            } else {
                fail "error";
            }
        } else {
            let x = 7;
            if (x == 7) {
                return true;
            } else {
                fail "error";
            }
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

    operation NestedResultIfReturn(b : Bool, result : Result) : Bool {
        if (b) {
            if (result == One) {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
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

    operation SetLocal(result : Result) : Unit {
        if (result == One) {
            mutable b = false;
            set b = true;
        }
    }

    operation SetReusedName(result : Result) : Unit {
        mutable b = false;
        if (result == One) {
            if (true) {
                // Re-declaring b is an error, but it shouldn't affect the invalid sets below.
                mutable b = false;
                set b = true;
            }
            set b = true;
        }
    }

    operation SetTuple(result : Result) : Unit {
        mutable a = false;
        if (result == One) {
            mutable b = 0;
            mutable c = 0.0;
            set (c, (b, a)) = (1.0, (1, true));
        }
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

    // Tuples and arrays currently don't support equality comparison, but result comparison should still be prevented if
    // they do.

    function ResultTuple(rr : (Result, Result)) : Bool {
        return rr == (One, One) ? true | false;
    }

    function ResultArray(rs : Result[]) : Bool {
        return rs == [One] ? true | false;
    }

    // Test references to operations in libraries.

    operation CallLibraryBqf(q : Qubit) : Unit {
        LibraryBqf(q);
    }

    operation ReferenceLibraryBqf() : (Qubit => Unit) {
        let f = LibraryBqf;
        return f;
    }

    operation CallLibraryBmf(q : Qubit) : Unit {
        LibraryBmf(q);
    }

    operation CallLibraryBmfWithNestedCall(q : Qubit) : Unit {
        LibraryBmfWithNestedCall(q);
    }

    operation ReferenceLibraryBmf() : (Qubit => Unit) {
        let f = LibraryBmf;
        return f;
    }

    operation CallLibraryFull(q : Qubit) : Unit {
        LibraryFull(q);
    }

    operation CallLibraryFullWithNestedCall(q : Qubit) : Unit {
        LibraryFullWithNestedCall(q);
    }

    operation ReferenceLibraryFull() : (Qubit => Unit) {
        let f = LibraryFull;
        return f;
    }
}

namespace Microsoft.Quantum.Intrinsic {
    operation X(q : Qubit) : Unit {
        body intrinsic;
    }

    operation M(q : Qubit) : Result {
        body intrinsic;
    }
}
