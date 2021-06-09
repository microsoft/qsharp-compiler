// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Overrides {

    newtype udt0 = (Result, Result);


    function emptyFunction () : Unit {
        body intrinsic;
    }

}


namespace Microsoft.Quantum.Testing {

    open Microsoft.Quantum.Intrinsic;


    // Nothing in it
    function emptyFunction () : Unit {
        body intrinsic;
    }


    operation emptyOperation () : Unit {
        body intrinsic;
    }


    //Function tests
    function intFunction () : Int {

        return 1;
    }


    // A duplicated H, just in case...
    operation H (q1 : Qubit) : Unit {

    }


    function powFunction (x : Int, y : Int) : Int {

        return x ^ y;
    }


    function bigPowFunction (x : BigInt, y : Int) : BigInt {

        return x ^ y;
    }


    operation zeroQubitOperation () : Unit
    {
        body { }
        adjoint auto;
        controlled auto;
        adjoint controlled auto;
    }

    operation oneQubitAbstractOperation (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation oneQubitSelfAdjointAbstractOperation (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint self;
        controlled intrinsic;
        controlled adjoint self;
    }

    newtype Basis = Pauli;

    newtype udt_Real = Double;

    newtype udt_Complex = (udt_Real, udt_Real);

    newtype udt_TwoDimArray = Result[][];

    operation randomAbstractOperation (q1 : Qubit, b : Basis, t : (Pauli, Double[][], Bool), i : Int) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation oneQubitSelfAdjointOperation (q1 : Qubit) : Unit {

        body (...) {
            Z(q1);
        }

        adjoint self;
    }


    operation oneQubitOperation (q1 : Qubit) : Unit {

        body (...) {
            // some comment in body.
            X(q1);
        }

        adjoint (...) {
            // some comment in adjoint.
            // second comment in adjoint.
            Adjoint X(q1);
        }

        controlled (c, ...) {
            Controlled X(c, q1);
            // some comment in controlled at the bottom.
            // Notice an empty statement (;) will be added to
            // make it easy to add this comment...
        }

        controlled adjoint (c, ...) {
            Adjoint Controlled X(c, q1);
        }
    }


    operation twoQubitOperation (q1 : Qubit, t1 : (Qubit, Double)) : Unit {

        body (...) {
            let (q2, r) = t1;
            CNOT(q1, q2);
            R(r, q1);
        }

        adjoint (...) {
            let (q2, r) = t1;

            // One Comment.
            Adjoint R(r, q1);

            // First comment.
            // Second comment.
            Adjoint CNOT(q1, q2);
        }
    }


    operation three_op1 (q1 : Qubit, q2 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation threeQubitOperation (q1 : Qubit, q2 : Qubit, arr1 : Qubits) : Unit {

        body (...) {
            three_op1(q1, q2);
            three_op1(q2, q1);
            three_op1(q1, q2);
        }

        adjoint (...) {
            Adjoint three_op1(q1, q2);
            Adjoint three_op1(q2, q1);
            Adjoint three_op1(q1, q2);
        }

        controlled (c, ...) {
            Controlled three_op1(c, (q1, q2));
            Controlled three_op1(c, (q2, q1));
            Controlled three_op1(c, (q1, q2));
        }

        controlled adjoint (c, ...) {
            Adjoint Controlled three_op1(c, (q1, q2));
            Adjoint Controlled three_op1(c, (q2, q1));
            Adjoint Controlled three_op1(c, (q1, q2));
        }
    }


    operation nestedArgTuple1 ((a : Int, b : Int), (c : Double, d : Double)) : Unit {
        body intrinsic;
    }


    operation nestedArgTuple2 (a : (Int, Int), (c : Double, (b : Int, d : (Qubit, Qubit)), e : Double)) : Unit {
        body intrinsic;
    }


    operation nestedArgTupleGeneric<'A> (a : ('A, Int), (c : 'A, (b : Int, d : (Qubit, 'A)), e : Double)) : Unit {
        body intrinsic;
    }


    // calling function with the same name in different namespaces
    operation duplicatedDefinitionsCaller () : Unit {

        emptyFunction();
        Microsoft.Quantum.Overrides.emptyFunction();

        using (qubits = Qubit[1]) {
            H(qubits[0]);
            Microsoft.Quantum.Intrinsic.H(qubits[0]);
        }
    }


    operation da_op0 () : Unit {
        body intrinsic;
    }


    operation da_op1 (q1 : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation da_op2 (i : Int, q : Qubit) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation da_op3 (d : Double, r : Result, i : Int) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation differentArgsOperation (q1 : Qubit, q2 : Qubit, arr1 : Qubit[]) : Unit {

        da_op0();
        Adjoint da_op1(q1);
        Controlled da_op2([q1], (1, q2));
        Adjoint Controlled da_op3([q1, q2], (1.1, One, Length(arr1)));
    }


    function random_f0 () : Int {

        return 1;
    }


    function random_f1 (n1 : Int, n2 : Int) : Int {

        return n1 * n2 - random_f0();
    }


    operation random_op0 (q1 : Qubit, i1 : Int) : Unit {

    }


    operation random_op1 (q1 : Qubit) : Result {
        body intrinsic;
    }


    operation random_op2 (q1 : Qubit) : Result {
        body intrinsic;
    }


    operation random_op3 (q1 : Qubit, r1 : Result, p1 : Pauli) : Unit {
        body intrinsic;
    }


    operation random_op4 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
    }


    operation random_op5 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
        adjoint intrinsic;
    }


    operation random_op6 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
    }


    operation random_op7 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation random_op8 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
    }


    operation random_op9 (q1 : Qubit, p1 : Pauli) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation random_op10 (q1 : Qubit, i1 : Int) : Unit {
        body intrinsic;
    }


    operation randomOperation (q1 : Qubit, i1 : Int, r1 : Result, p1 : Pauli) : Unit {

        body (...) {
            let arr1 = [q1, q1];

            using (qubits = Qubit[Length(arr1)]) {
                random_op0(q1, i1);
                let r = random_op1(q1);

                if (r == One) {

                    borrowing (b = Qubit[Length(arr1) + i1]) {

                        for (i in 0 .. 1 .. 5) {
                            let m = random_op2(arr1[random_f1(1, 2)]);

                            if (m == Zero) {
                                random_op0(q1, i);
                            }
                        }

                        random_op3(q1, r1, p1);
                    }
                }
            }
        }

        adjoint (...) {
            random_op0(q1, i1);
            random_op6(q1, p1);
            Adjoint random_op5(q1, p1);
        }

        controlled (c, ...) {
            random_op0(q1, i1);
            random_op4(q1, p1);
            Controlled random_op7(c, (q1, p1));
        }

        controlled adjoint (c, ...) {
            random_op10(q1, i1);
            random_op8(q1, p1);
            Adjoint Controlled random_op9(c, (q1, p1));
        }
    }


    function if_f0 () : Int {

        return 0;
    }


    operation ifOperation (i : Int, r : Result, p : Pauli) : Int {

        mutable n = 0;

        if (r == One) {
            set n = if_f0() * i;
        }

        if (p == PauliX) {
            return n;
        }
        else {
            return 0;
        }

        if (p == PauliX) {
            return n;
        }
        elif (p == PauliY) {
            return 1;
        }
        else {
            return p == PauliI ? 3 | if_f0();
        }
    }


    function foreach_f2 (n1 : Int, n2 : Int) : Int {

        return n1 * n2;
    }


    operation foreachOperation (i : Int, r1 : Result) : Result {

        mutable result = 0;

        for (n in 0 .. i) {
            set result = result + i;
        }

        for (n in i .. -1 .. 0) {
            set result = (result - i) * 2;
        }

        let range = 0 .. 10;

        for (n in range) {
            set result = RangeEnd(range) + result + n * -foreach_f2(n, 4);
        }

        if (result > 10) {
            return One;
        }
        else {
            return Zero;
        }
    }

    newtype udt_args0 = Qubit[];

    newtype udt_args1 = (Int, Qubit[]);

    newtype udt_args2 = (udt_args0 => udt_args1);

    newtype udt_args3 = (udt_args0 => udt_args1 is Ctl);

    newtype udt_args4 = (udt_args0 => udt_args1 is Adj);

    newtype udt_args5 = (udt_args0 => udt_args1 is Adj + Ctl);

    newtype udt_args1_0 = (Int, udt_args0);

    newtype udt_args1_1 = (Int, udt_args1);

    newtype udt_args1_2 = (Int, udt_args2);

    newtype udt_args2_0 = (Int, Result, udt_args0[]);

    newtype udt_args2_1 = (Int, Result, udt_args1[]);

    newtype udt_args2_2 = (Int, Result, udt_args2[]);


    operation udtsTest
    (
        qubits : Qubit[],
        u0 : Microsoft.Quantum.Overrides.udt0,
        u1 : udt_args1,
        u2 : udt_args2,
        u3 : udt_args3,
        u4 : udt_args4,
        u5 : udt_args5,
        op0 : (udt_args0 => Unit),
        op1 : (udt_args1 => Unit is Adj + Ctl),
        op2 : (udt_args0 => udt_args1 is Adj + Ctl),
        op1_0 : (udt_args1_0 => Unit is Adj),
        op1_1 : (udt_args1_1 => Unit is Ctl),
        op1_2 : (udt_args1_2 => Unit), op2_0 : (udt_args2_0 => Unit),
        op2_1 : (udt_args2_1 => Unit), op2_2 : (udt_args2_2 => Unit is Adj + Ctl),
        op_o : (Microsoft.Quantum.Overrides.udt0 => Unit)
    ) : udt_args1 {

        let args0 = udt_args0(qubits);
        let args1 = udt_args1(1, args0!);
        let args1a = op2(args0);
        let args2 = udt_args2(op2);
        let args3 = udt_args3(op2);
        let args4 = udt_args4(op2);
        let ext0 = Microsoft.Quantum.Overrides.udt0(Zero, One);
        op0(args0);
        op0(udt_args0(qubits));
        op1(args1);
        op1(udt_args1(2, qubits));
        op1(udt_args1(3, args0!));
        op1(udt_args1(4, (udt_args0(qubits))!));
        return udt_args1(22, qubits);
    }



    newtype returnUdt0 = (Int, Int);

    newtype returnUdt1 = (Int, Int)[];

    newtype returnUdt3 = returnUdt0[];

    function returnTest1 () : Unit {

        return ();
    }


    function returnTest2 () : Int {

        return 5;
    }


    function returnTest3 () : (Int, Int) {

        return (5, 6);
    }


    function returnTest4 () : returnUdt0 {

        return returnUdt0(7, 8);
    }


    function returnTest5 () : Int[] {

        return [9, 10];
    }


    function returnTest6 () : returnUdt1 {

        return returnUdt1([(1, 2), (3, 4)]);
    }


    function returnTest7 () : returnUdt0[] {

        return [returnUdt0(1, 2), returnUdt0(3, 4)];
    }


    function returnTest8 () : returnUdt3 {

        return returnUdt3([returnUdt0(1, 2), returnUdt0(3, 4)]);
    }


    function returnTest9 () : (returnUdt0, returnUdt1) {

        return (returnUdt0(7, 8), returnUdt1([(1, 2), (3, 4)]));
    }


    function returnTest10 () : Microsoft.Quantum.Overrides.udt0 {

        return Microsoft.Quantum.Overrides.udt0(Zero, One);
    }

    newtype repeat_udt0 = (Int, Qubit[]);


    operation repeat_op0 (info : repeat_udt0) : Result {
        body intrinsic;
    }


    operation repeat_op1 (index : Int, qubits : Qubit[]) : Result {
        body intrinsic;
    }


    operation repeat_op2 (angle : Double, info : repeat_udt0) : Result {
        body intrinsic;
    }


    operation repeatOperation (i : Int) : Unit {

        using (qubits = Qubit[i]) {

            repeat {
                mutable res = repeat_op0(repeat_udt0(0, qubits));
            }
            until (repeat_op1(0, qubits) == One)
            fixup {
                set res = repeat_op2(3.0, repeat_udt0(i - 1, qubits));
            }
        }
    }


    newtype udtTuple_1 = (Int, Qubit);

    newtype udtTuple_2 = (Int, (Qubit, Qubit));

    operation udtTuple (i : Int, t1 : udtTuple_1, t2 : udtTuple_2, q : Qubit) : Unit {
        body intrinsic;
    }


    operation selfInvokingOperation (q1 : Qubit) : Unit {

        body (...) {
            Z(q1);
        }

        adjoint (...) {
            Adjoint Z(q1);
            selfInvokingOperation(q1);
        }
    }


    function factorial (x : Int) : Int {

        if (x == 1) {
            return 1;
        }
        else {
            return x * factorial(x - 1);
        }
    }


    function let_f0 (n : Int) : Range {
        body intrinsic;
    }

    newtype let_udt_1 = (Int, Qubit[]);

    newtype let_udt_2 = (let_udt_1 => Range is Adj + Ctl);

    newtype let_udt_3 = (let_udt_1 => let_udt_2);


    operation letsOperations (q1 : Qubit, n : Int, udts : (Microsoft.Quantum.Overrides.udt0, let_udt_1, let_udt_2, let_udt_3, (Qubit => Unit))) : Range {

        // Assigning from identifier:
        let q2 = q1;

        // Assigning from op result:
        let r = M(q1);

        // Assigning from literal:
        let i = 1.1;
        let iZero = 0;
        let dZero = 0.0;

        // Assigning from ranges:
        let a = 0 .. 10;
        let b = 8 .. -1 .. 5;

        // Assigning from expressions
        // Simple and complex:
        let j = n + 1;
        let k = ((n - 1) * (n ^ 2) / 3) % 4;

        // Deconstructing tuples:
        let t = (2.2, (3, One));
        let (l, (m, o)) = t;
        let (p, q) = t;
        let (u0, u1, u2, u3, call1) = udts;
        let u = u3!(u1);

        // Deconstructing inside inner blocks:
        if (true) {
            let (l2, (m2, o2)) = t;
            return (u3!(u1))!(u1);
        }

        // Interpolated string literal
        let s = $"n is {n} and u is {u3!(u1)}, {r}, {n}, {j}";
        let str = $"Hello{true ? "quantum" | ""} world!";

        //let str2 = "more complicated stuff { true ? $"{n}" | "" }"; // to be fixed in the compilation builder...

        // Discarding variables:
        let (l3, _) = t;
        let (_, (_, o3)) = t;
        let (_, (m3, _)) = t;
        let _ = t;
        let _ = t;
        return let_f0(n);
    }


    function bitOperations (a : Int, b : Int) : Bool {

        let andEx = a &&& b;
        let orEx = a ||| b;
        let xorEx = a ^^^ b;
        let left = a <<< b;
        let right = a >>> b;
        let negation = ~~~a;
        let total = ((((andEx + orEx) + xorEx) + left) + right) + negation;

        if (total > 0) {
            return true;
        }
        else {
            return false;
        }
    }

    newtype arrays_T1 = Pauli[];

    newtype arrays_T2 = (Pauli[], Int[]);

    newtype arrays_T3 = Result[][];

    operation arraysOperations (qubits : Qubit[], register : Qubits, indices : Range[][], t : arrays_T3) : Result[][] {

        // Creating/Assigning arrays
        let q = qubits;
        let r1 = [Zero];
        let r2 = [0, 1];
        let r3 = [0.0, 1.1, 2.2];
        let r4 = [r2[0], r2[1], 2];
        let r5 = new Result[4 + 2];
        mutable r6 = new Pauli[Length(r5)];
        let r7 = r2 + r4;
        let r8 = r7[1 .. 5 .. 10];
        let r9 = arrays_T1([PauliX, PauliY]);
        let r10 = new arrays_T1[4];
        let r11 = arrays_T2([PauliZ], [4]);
        let r12 = new arrays_T2[Length(r10)];
        let r13 = arrays_T3([[Zero, One], [One, Zero]]);
        let r14 = qubits + register!;
        let r15 = (register!)[0 .. 2];
        let r16 = qubits[1 .. -1];
        let r18 = new Qubits[2];
        let r19 = new Microsoft.Quantum.Overrides.udt0[7];

        // Accessing array items:
        let i0 = ((r13!)[0])[1];
        let i1 = r2[0 + Length(r1)];
        let i2 = r3[i1 * ((2 + 3) - 8 % 1)];
        let i3 = qubits[0];
        let i4 = indices[0];
        let i5 = (indices[0])[1];
        let i6 = (t!)[0];
        let i7 = (register!)[3];

        // Lengths:
        let l0 = Length(qubits);
        let l1 = Length(indices);
        let l2 = Length(indices[0]);
        let l3 = Length(t!);
        let l4 = Length(r8);
        let l5 = Length(r9!);
        let l6 = Length(register!);

        return [[i0, One], [Zero]];
    }


    function GetMeARange () : Range {

        return 0 .. 1;
    }


    operation sliceOperations (qubits : Qubit[], option : Int) : Qubit[] {

        let r2 = 10 .. -2 .. 0;
        let ranges = new Range[1];
        let s1 = qubits[0 .. 10];
        let s2 = qubits[r2];
        let s3 = qubits[ranges[3]];
        let s4 = qubits[GetMeARange()];

        return qubits[10 .. -3 .. 0];
    }


    operation rangeOperations (r: Range) : Int
    {
        return RangeStart(r) + RangeEnd(r) + RangeStep(r);
    }

    function call_target1
    (
        i : Int,
        plain : (Qubit => Unit),
        adj : (Qubit => Unit is Adj),
        ctr : (Qubit => Unit is Ctl),
        uni : (Qubit => Unit is Adj + Ctl)
    ) : Unit { }


    function call_target2
    (
        i : Int, plain : (Result, (Qubit => Unit)),
        adj : (Result, (Qubit => Unit is Adj)),
        ctr : (Result, (Qubit => Unit is Ctl)),
        uni : (Result, (Qubit => Unit is Adj + Ctl))
    ) : Unit { }

    newtype call_plain = (Qubit => Unit);

    newtype call_adj = (Qubit => Unit is Adj);

    newtype call_ctr = (Qubit => Unit is Ctl);

    newtype call_uni = (Qubit => Unit is Adj + Ctl);

    operation callTests (qubits : Qubits) : Unit {

        let plain = call_plain(X);
        let adj = call_adj(X);
        let ctr = call_ctr(X);
        let uni = call_uni(X);
        X(qubits![0]);
        Adjoint X(qubits![0]);
        Controlled X(qubits![1 .. 5], qubits![0]);
        call_target1(1, X, X, X, X);
        call_target1(1, plain!, adj!, ctr!, uni!);
        call_target2(1, (Zero, X), (Zero, X), (Zero, X), (Zero, X));
        call_target2(2, (One, plain!), (One, adj!), (One, ctr!), (One, uni!));
    }


    operation helloWorld (n : Int) : Int {

        let r = n + 1;
        return r;
    }


    operation alloc_op0 (q1 : Qubit) : Unit {
        body intrinsic;
    }


    operation allocOperation (n : Int) : Unit {

        body (...) {
            using (q = Qubit()) {
                let flag = true;
                (flag ? X | Z)(q);
                alloc_op0(q);
            }

            using (qs = Qubit[n]) {
                alloc_op0(qs[n - 1]);
            }

            using ((q1, (q2, (_, q3, _, q4))) = (Qubit(), ((Qubit(), Qubit[2]), (Qubit(), Qubit[n], Qubit[n - 1], Qubit[4])))) {
                alloc_op0(q1);
                alloc_op0(q3[1]);
            }
        }

        adjoint (...) {
            borrowing (b = Qubit[n]) {
                alloc_op0(b[n - 1]);
            }

            borrowing ((q1, (q2, (_, q3))) = (Qubit(), (Qubit[2], (Qubit(), (Qubit[n], Qubit[4]))))) {

                using (qt = (Qubit(), (Qubit[1], Qubit[2]))) {
                    let (qt1, qt2) = qt;
                    alloc_op0(qt1);
                }

                alloc_op0(q1);
                alloc_op0(q2[1]);
            }
        }
    }


    operation failedOperation (n : Int) : Int {

        fail "This operation should never be called.";
        return 1;
    }


    operation compareOps (n : Int) : Bool {

        let lt = n < 1;
        let lte = n <= 2;
        let gt = n > 3;
        let gte = n >= 4;
        return (lt == (lte and gt)) != gte or not lt;
    }


    operation partialGeneric1<'A, 'B> (a : 'A, b : 'B, c : ('A, 'B)) : Unit {
        body intrinsic;
    }


    operation partialGeneric2<'A, 'B, 'C, 'D> (a : 'A, b : 'B, c : ('C, 'D)) : Unit {
        body intrinsic;
    }


    operation partial1Args (a : Int) : Unit {
        body intrinsic;
    }


    operation partialInnerTuple (a : Int, b : (Double, Result)) : Unit {
        body intrinsic;
    }


    operation partial3Args (a : Int, b : Double, c : Result) : Unit {
        body intrinsic;
    }


    operation partialNestedArgsOp (a : (Int, Int, Int), b : ((Double, Double), (Result, Result, Result))) : Unit {
        body intrinsic;
    }


    function partialFunction (a : Int, b : Double, c : Pauli) : Result {
        return Zero;
    }


    // TODO: (partial1Args (_))(1);


    operation partialApplicationTest
    (
        i : Int, res : Result,
        partialInput : ((Int, (Double, Double), (Result, Result, Result)) => Unit),
        partialUnitary : ((Double, ((Int, Double) -> Result), Qubit[]) => Unit is Adj + Ctl)
    ) : (Qubit[] => Unit is Adj + Ctl) {

        (partial3Args(_, _, _))(1, 3.5, One);
        (partial3Args(1, _, Zero))(3.5);
        (partial3Args(_, 3.5, _))(1, Zero);
        (partial3Args(1, 3.5, _))(Zero);
        (partial3Args(1, _, _))(3.5, Zero);
        (partialInnerTuple(_, _))(1, (3.5, One));
        (partialInnerTuple(_, (_, _)))(1, (3.5, Zero));
        (partialInnerTuple(1, _))(3.5, Zero);
        (partialInnerTuple(_, (3.5, _)))(1, Zero);
        (partialInnerTuple(_, (_, One)))(1, 3.5);
        (partialInnerTuple(1, (3.5, _)))(One);
        ((partialNestedArgsOp(_, _))((1, i, _), (_, (res, _, res))))(1, ((3.3, 2.0), Zero));
        ((partialNestedArgsOp((1, i, _), ((_, _), (res, _, res))))(2, ((2.2, _), _)))(3.3, Zero);
        ((partialNestedArgsOp((i, _, 1), ((_, 1.0), (res, _, Zero))))(i, (_, res)))(3.3);
        (partialGeneric1(0, Zero, (_, One)))(1);
        (partialGeneric1(_, _, (1, One)))(0, Zero);
        (partialGeneric1(0, _, (1, _)))(Zero, One);
        (partialGeneric2(0, Zero, (_, One)))(1);
        (partialGeneric2(_, _, (1, One)))(0, Zero);
        (partialGeneric2(0, _, (1, _)))(Zero, One);
        (partialInput(1, (_, 1.1), (Zero, _, _)))(2.2, (One, One));
        return partialUnitary(1.1, partialFunction(_, _, PauliX), _);
    }

    newtype Q = Qubit;

    newtype U = (Qubit => Unit is Adj + Ctl);

    newtype A = (Int -> Int[]);

    newtype B = (Int[] -> U);

    newtype C = (Int, A);

    newtype D = (Int[] -> U);

    newtype E = (Double -> C);

    newtype F = (D, E);

    newtype G = ((Double, F, Qubit[]) => Unit is Adj + Ctl);

    newtype AA = A;

    newtype QQ = Q;

    function partialFunctionTest
    (
        start : Double,
        t1 : (A, D),
        t2 : (B, E),
        op : G
    ) : (Qubit[] => Unit is Adj + Ctl) {

        let r1 = (partialFunction(_, _, _))(2, 2.2, PauliY);
        let r2 = ((partialFunction(1, _, _))(3.3, _))(PauliZ);
        let (a, d) = t1;
        let (b, e) = t2;
        let f = F(d, e);
        return op!(start, f, _);
    }


    operation OP_1 (q : Qubit) : Result {
        body intrinsic;
    }


    operation opParametersTest
    (
        q1 : Qubit,
        op0 : (Qubit => Result),
        op1 : ((Qubit => Result) => Unit),
        op2 : ((Qubit, Qubit) => Unit is Adj),
        t1 : (((Qubit[], (Qubit, Qubit)) => Unit is Ctl), ((Qubit[], (Qubit, Qubit)) => Unit is Adj + Ctl)),
        f1 : (Double -> Double)
    ) : (Qubit => Unit) {

        op1(OP_1);
        let v0 = op0;
        let r0 = v0(q1);
        let (op3, op4) = t1;
        op3([q1], (q1, q1));
        return op2(q1, _);
    }


    operation With1C (outerOperation : (Qubit => Unit is Adj), innerOperation : (Qubit => Unit is Ctl), target : Qubit) : Unit {

        body (...) {
            outerOperation(target);
            innerOperation(target);
            Adjoint outerOperation(target);
        }

        controlled (controlRegister, ...) {
            outerOperation(target);
            Controlled innerOperation(controlRegister, target);
            Adjoint outerOperation(target);
        }
    }


    operation measureWithScratch (pauli : Pauli[], target : Qubit[]) : Result {

        mutable result = Zero;

        using (scratchRegister = Qubit[1]) {
            let scratch = scratchRegister[0];

            for (idxPauli in 0 .. Length(pauli) - 1) {
                let P = pauli[idxPauli];
                let src = [target[idxPauli]];

                if (P == PauliX) {
                    Controlled X(src, scratch);
                }
                elif (P == PauliY) {
                    Controlled (With1C(S, X, _))(src, scratch);
                }
                elif (P == PauliZ) {
                    Controlled (With1C(Microsoft.Quantum.Intrinsic.H, X, _))(src, scratch);
                }
            }

            set result = M(scratch);
        }

        return result;
    }


    operation noOpResult (r : Result) : Unit {

        body (...) {
        }

        adjoint invert;
        controlled distribute;
        controlled adjoint distribute;
    }


    operation noOpGeneric<'T> (r : 'T) : Unit {

        body (...) {
        }

        adjoint invert;
        controlled distribute;
        controlled adjoint distribute;
    }

    operation Hold<'T> (op : ('T => Unit), arg : 'T, dummy : Unit) : Unit
    {
        op(arg);
    }

    function iter<'T, 'U> (mapper : ('T -> 'U), source : 'T[]) : Unit {
        for (i in source) {
            let v = mapper(i);
        }
    }


    function testLengthDependency() : Unit {
        iter(Length<Result>, [[One], [Zero, One]]);
    }
}

// Notice most Intrinsics are defined in Intrinsic.qs
// We have these here for testing.
namespace Microsoft.Quantum.Intrinsic {
    operation CNOT (q1 : Qubit, q2 : Qubit) : Unit {
        body intrinsic;
        adjoint auto;
    }
}

namespace Microsoft.Quantum.Core
{
    function Length<'T> (a : 'T[]) : Int {
        body intrinsic;
    }


    function RangeStart (range : Range) : Int {
        body intrinsic;
    }


    function RangeEnd (range : Range) : Int {
        body intrinsic;
    }


    function RangeStep (range : Range) : Int {
        body intrinsic;
    }
}



namespace Microsoft.Quantum.Compiler.Generics {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Testing;


    function genF1<'A> (arg : 'A) : Unit {
        body intrinsic;
    }


    operation genMapper<'T, 'U> (mapper : ('T => 'U), source : 'T[]) : 'U[] {

        mutable result = new 'U[Length(source)];

        for (i in 0 .. Length(source) - 1) {
            let m = mapper(source[i]);
            set result = result w/ i <- m;
        }

        return result;
    }


    operation genIter<'T> (callback : ('T => Unit is Adj + Ctl), source : 'T[]) : Unit {

        body (...) {
            for (i in 0 .. Length(source) - 1) {
                callback(source[i]);
            }
        }

        adjoint invert;
        controlled distribute;
        controlled adjoint distribute;
    }


    operation genC1<'T> (a1 : 'T) : Unit {
        body intrinsic;
    }


    operation genC1a<'T> (a1 : 'T) : 'T {
        body intrinsic;
    }


    operation genC2<'T, 'U> (a1 : 'T) : 'T {
        body intrinsic;
    }


    operation genAdj1<'T> (a1 : 'T) : Unit {
        body intrinsic;
        adjoint self;
    }


    operation genU1<'T> (a1 : 'T) : Unit {

        body (...) {
            let x = genC2<'T, Unit>(a1);
        }

        adjoint  (...) { }
        controlled (c, ...) { }
        controlled adjoint (c, ...) { }
    }


    operation genCtrl3<'X, 'Y, 'Z> (arg1 : 'X, arg2 : (Int, ('Y, 'Z), Result)) : Unit {
        body intrinsic;
        controlled intrinsic;
    }


    operation genU2<'A, 'B> (a1 : 'A, t1 : ('A, 'B), i : Int) : Unit {
        body intrinsic;
        adjoint intrinsic;
        controlled intrinsic;
        controlled adjoint intrinsic;
    }


    operation ResultToString (v : Result) : String {

        if (v == One) {
            return "uno";
        }
        else {
            return "zero";
        }
    }


    operation usesGenerics () : Unit {

        let a = [One, Zero, Zero];
        let s = [ResultToString(a[0]), ResultToString(a[1])];
        noOpResult(a[0]);

        using (qubits = Qubit[3]) {

            let op = Hold(CNOT, (qubits[0], qubits[1]),_);
            op();

            noOpGeneric(qubits[0]);
            noOpGeneric(a[0]);
            genIter(X, qubits);
        }

        genIter(noOpResult, a);
        genIter(genU1<String>, genMapper(ResultToString, a));
        genIter(genU1<String>, s);
        genIter(genU1<Result>, a);
    }

    operation genericWithMultipleTypeParams<'A, 'B, 'C>() : Unit { }

    operation callsGenericWithMultipleTypeParams () : Unit {
        genericWithMultipleTypeParams<Double, Int, Int>();
    }

    operation composeImpl<'A, 'B> (second : ('A => Unit), first : ('B => 'A), arg : 'B) : Unit {

        second(first(arg));
    }


    operation compose<'A, 'B> (second : ('A => Unit), first : ('B => 'A)) : ('B => Unit) {

        return composeImpl(second, first, _);
    }


    function genRecursion<'T> (x : 'T, cnt : Int) : 'T {

        if (cnt == 0) {
            return x;
        }
        else {
            return genRecursion(x, cnt - 1);
        }
    }


    function MapDefaults<'C, 'B> (map : ('C -> 'C), l : Int) : Unit {

        mutable arr = new 'C[l];

        for (i in 0 .. l - 1) {
            set arr = arr w/ i <- map(arr[i]);
        }
    }

    newtype MyType1 = (i1 : Int[], i2 : Double);
    newtype MyType2 = (i1 : Int, i2 : MyType1, (i3 : Int[], i4 : String));

    function UpdateUdtItems (udt : MyType2) : MyType2 {

        mutable arr = new Int[10];
        return udt
            w/ i1 <- -5
            w/ i3 <- arr
            w/ i1 <- 1;
    }

    newtype NamedTuple = (FirstItem: (Int, Double), SecondItem: Int);

    // Access Modifiers

    internal function EmptyInternalFunction () : Unit { }

    internal operation EmptyInternalOperation () : Unit { }

    internal newtype InternalType = Unit;

    internal operation MakeInternalType () : InternalType {
        return InternalType();
    }

    operation UseInternalCallables () : Unit {
        EmptyInternalFunction();
        EmptyInternalOperation();
        let x = InternalType();
        let y = MakeInternalType();
    }
}
