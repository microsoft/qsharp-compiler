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

    function Recursion1() : Unit {
        Recursion1();
    }

    function Recursion2A() : Unit {
        Recursion2B();
    }

    function Recursion2B() : Unit {
        Recursion2A();
    }

    function Fail() : Unit {
        fail "test";
    }

    operation Repeat() : Unit {
        repeat {} until false;
    }

    function While() : Unit {
        while false {}
    }

    function TwoReturns() : Bool {
        if true {
            return true;
        }

        return false;
    }

    function UseBigInt() : Unit {
        let x = 0L;
    }

    function ReturnBigInt() : BigInt {
        return 0L;
    }

    function ConditionalBigInt(b : Bool) : Unit {
        let x = b ? 0L | 1L;
    }

    function MessageStringVar() : Unit {
        let x = "foo";
        Message(x);
    }

    function ReturnString() : String {
        return "foo";
    }

    function ConditionalString(b : Bool) : Unit {
        let x = b ? "foo" | "bar";
    }

    function TakeString(s : String) : Unit {
        body intrinsic;
    }

    function UseStringArg() : Unit {
        TakeString("foo");
    }

    function MessageStringLit() : Unit {
        Message("foo");
    }

    function MessageInterpStringLit() : Unit {
        let x = 0;
        Message($"x = {x}");
    }

    function UseRangeVar() : Unit {
        let xs = 0..10;
        for x in xs {}
    }

    function UseRangeLit() : Unit {
        for x in 0..10 {}
    }

    function MutableToLet() : Unit {
        mutable x = 0;
        let y = x;
    }

    function LetToLet() : Unit {
        let x = 0;
        let y = x;
    }

    function ParamToLet(x : Int) : Unit {
        let y = x;
    }

    function MutableToMutable() : Unit {
        mutable x = 0;
        mutable y = x;
    }

    function LetToMutable() : Unit {
        let x = 0;
        mutable y = x;
    }

    function TakeBool(b : Bool) : Unit {}

    function MutableToCall() : Unit {
        mutable x = true;
        TakeBool(x);
    }

    function LetToCall() : Unit {
        let x = true;
        TakeBool(x);
    }

    function MutableArray() : Unit {
        mutable xs = [0, 1];
    }

    function LetArray() : Unit {
        let xs = [0, 1];
    }

    function MutableArrayLitToFor() : Unit {
        mutable x = 0;
        for y in [x] {}
    }

    function LetArrayLitToFor() : Unit {
        let x = 0;
        for y in [x] {}
    }

    function MutableArrayToFor() : Unit {
        mutable xs = [0, 1];
        for x in xs {}
    }

    function LetArrayToFor() : Unit {
        let xs = [0, 1];
        for x in xs {}
    }

    function MutableRangeLitToFor() : Unit {
        mutable x = 0;
        for i in x..1 {}
    }

    function LetRangeLitToFor() : Unit {
        let x = 0;
        for i in x..1 {}
    }

    function MutableRangeToFor() : Unit {
        mutable r = 0..1;
        for i in r {}
    }

    function LetRangeToFor() : Unit {
        let r = 0..1;
        for i in r {}
    }

    function MutableToArraySize() : Unit {
        mutable x = 3;
        let _ = [0, size = x];
    }

    function LetToArraySize () : Unit {
        let x = 3;
        let _ = [0, size = x];
    }

    function NewArray<'a>() : 'a[] {
        return new 'a[1];
    }

    function MutableToNewArraySize() : Unit {
        mutable x = 3;
        let _ = new Int[x];
    }

    function LetToNewArraySize() : Unit {
        let x = 3;
        let _ = new Int[x];
    }

    function MutableToArrayIndex(xs : Int[]) : Unit {
        mutable i = 0;
        mutable x = xs[i];
    }

    function LetToArrayIndex(xs : Int[]) : Unit {
        let i = 0;
        let x = xs[i];
    }

    function MutableToArraySlice(xs : Int[]) : Unit {
        mutable r = 0..1;
        mutable ys = xs[r];
    }

    function LetToArraySlice(xs : Int[]) : Unit {
        let r = 0..1;
        let ys = xs[r];
    }

    function MutableToArrayIndexUpdate(xs : Int[]) : Unit {
        mutable i = 0;
        mutable ys = xs w/ i <- 1;
    }

    function LetToArrayIndexUpdate(xs : Int[]) : Unit {
        let i = 0;
        let ys = xs w/ i <- 1;
    }

    function MutableToArraySliceUpdate(xs : Int[]) : Unit {
        mutable r = 0..1;
        mutable ys = xs w/ r <- [1, 2];
    }

    function LetToArraySliceUpdate(xs : Int[]) : Unit {
        let r = 0..1;
        let ys = xs w/ r <- [1, 2];
    }

    function FunctionValue() : Unit {
        let f = TakeBool;
        f(true);
    }

    function FunctionExpression() : Unit {
        [TakeBool][0](true);
    }

    operation OperationValue() : Unit {
        let op = X;
        use q = Qubit();
        op(q);
    }

    operation OperationExpression(b : Bool) : Unit {
        use q = Qubit();
        [X][0](q);
    }

    @EntryPoint()
    operation EntryPointParamBool(x : Bool) : Result {
        return Zero;
    }

    @EntryPoint()
    operation EntryPointParamInt(x : Int) : Result {
        return Zero;
    }

    @EntryPoint()
    operation EntryPointParamDouble(x : Double) : Result {
        return Zero;
    }

    @EntryPoint()
    operation EntryPointReturnUnit() : Unit {}

    @EntryPoint()
    operation EntryPointReturnResult() : Result {
        return Zero;
    }

    @EntryPoint()
    operation EntryPointReturnBool() : Bool {
        return false;
    }

    @EntryPoint()
    operation EntryPointReturnInt() : Int {
        return 0;
    }

    @EntryPoint()
    operation EntryPointReturnDouble() : Double {
        return 0.0;
    }

    @EntryPoint()
    operation EntryPointReturnResultArray() : Result[] {
        return [];
    }

    @EntryPoint()
    operation EntryPointReturnBoolArray() : Bool[] {
        return [];
    }

    @EntryPoint()
    operation EntryPointReturnDoubleArray() : Double[] {
        return [];
    }

    @EntryPoint()
    operation EntryPointReturnResultTuple() : (Result, Result) {
        return (Zero, Zero);
    }

    @EntryPoint()
    operation EntryPointReturnResultBoolTuple() : (Result, Bool) {
        return (Zero, false);
    }

    @EntryPoint()
    operation EntryPointReturnResultDoubleTuple() : (Result, Double) {
        return (Zero, 0.0);
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

    operation ReferenceLibraryOverride(q : Qubit) : Unit {
        LibraryOverride(q);
    }
}

namespace Microsoft.Quantum.Intrinsic {
    operation X(q : Qubit) : Unit {
        body intrinsic;
    }

    operation M(q : Qubit) : Result {
        body intrinsic;
    }

    function Message(message : String) : Unit {
        body intrinsic;
    }
}
