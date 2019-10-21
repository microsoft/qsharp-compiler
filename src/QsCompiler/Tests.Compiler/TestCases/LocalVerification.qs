// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for expression and statement verification
namespace Microsoft.Quantum.Testing.LocalVerification {

    open Microsoft.Quantum.Testing.General;
    open Microsoft.Quantum.Testing.TypeChecking;


    // copy-and-update array

    function CopyAndUpdateArray1 (arr : Int[]) : Int[] {
        return arr w/ 0 <- 1;
    }

    function CopyAndUpdateArray2 (arr : (String, Double)[]) : (String, Double)[] {
        return arr w/ 0 <- ("", 1.);
    }

    function CopyAndUpdateArray3 (arr : Int[][]) : Int[][] {
        return arr w/ 0 <- new Int[10];
    }

    function CopyAndUpdateArray4<'T> (arr : 'T[]) : 'T[] {
        return arr w/ 0 <- (new 'T[1])[0];
    }

    function CopyAndUpdateArray5 (arr : Int[]) : Int[] {
        return arr w/ 0 <- 1.;
    }

    function CopyAndUpdateArray6 (arr : (String, Double)[]) : (String, Double)[] {
        return arr w/ 0 <- ("", "");
    }

    function CopyAndUpdateArray7 (arr : (String, Double)[]) : (String, Double)[] {
        return arr w/ 0 <- ("", (1., 1.));
    }

    function CopyAndUpdateArray8<'T> (arr : 'T[]) : 'T[] {
        return arr w/ 0 <- 1;
    }

    function CopyAndUpdateArray9 (arr : Int[]) : Int[] {
        return arr 
            w/ 0 <- 1
            w/ 1 <- 2;
    }

    function CopyAndUpdateArray10 (arr : Int[]) : Int[] {
        return arr w/ 0 .. 2 <- [1,1,1];
    }

    function CopyAndUpdateArray11<'T> (arr : ('T => Unit)[], op : ('T => Unit)) : ('T => Unit)[] {
        return arr w/ 0 <- op;
    }

    function CopyAndUpdateArray12<'T> (
        arr : ('T => Unit)[], 
        op1 : ('T => Unit), 
        op2 : ('T => Unit)) 
    : ('T => Unit)[] {
        return arr 
            w/ 0 <- op1
            w/ 1 <- op2;
    }

    function CopyAndUpdateArray13<'T> (
        arr : ('T => Unit)[], 
        op1 : ('T => Unit is Ctl), 
        op2 : ('T => Unit is Adj)) 
    : ('T => Unit)[] {
        return arr 
            w/ 0 <- op1
            w/ 1 <- op2;
    }

    function CopyAndUpdateArray14<'T> (
        arr : ('T => Unit is Adj)[], 
        op1 : ('T => Unit is Ctl + Adj), 
        op2 : ('T => Unit is Adj)) 
    : ('T => Unit is Adj)[] {
        return arr 
            w/ 0 <- op1
            w/ 1 <- op2;
    }

    function CopyAndUpdateArray15<'T> (
        arr : ('T => Unit is Adj)[], 
        op : ('T => Unit is Ctl)) 
    : ('T => Unit is Adj)[] {
        return arr w/ 0 <- op;
    }

    function CopyAndUpdateArray16<'T> (
        arr : ('T => Unit is Adj)[], 
        op : (Int => Unit is Adj)) 
    : ('T => Unit is Adj)[] {
        return arr w/ 0 <- op;
    }


    // update-and-reassign array

    function UpdateAndReassign1 () : Unit {
        mutable arr = new Int[10];
        set arr w/= 1 <- 0;
    }

    function UpdateAndReassign2 () : Unit {
        mutable arr = new Int[10];
        set arr w/= 0 .. 2 <- [0,0,0];
    }

    function UpdateAndReassign3 () : Unit {
        mutable arr = new Int[10];
        set arr w/= 0 .. Length(arr)-1 <- arr w/ 0 <- 1;
    }

    function UpdateAndReassign4 () : Unit {
        mutable arr = new Int[10];
        set arr w/= 1 <- 0.;
    }

    function UpdateAndReassign5 () : Unit {
        mutable arr = new Int[10];
        set arr w/= 0 .. Length(arr) <- 1 .. Length(arr);
    }

    function UpdateAndReassign6<'T> (op : ('T => Unit)) : Unit {
        mutable arr = new ('T => Unit)[10];
        set arr w/= 0 <- op;
    }

    function UpdateAndReassign7<'T> (op : ('T => Unit is Ctl)) : Unit {
        mutable arr = new ('T => Unit)[10];
        set arr w/= 0 <- op;
    }

    function UpdateAndReassign8<'T> (
        op1 : ('T => Unit is Ctl), 
        op2 : ('T => Unit is Adj)) 
    : Unit {
        mutable arr = new ('T => Unit)[10];
        set arr w/= 0 .. Length(arr) <- 
            arr w/ 0 <- op1 w/ 1 <- op2;
    }

    function UpdateAndReassign9<'T> (op : ('T => Unit)) : Unit {
        mutable arr = new (Int => Unit)[10];
        set arr w/= 0 <- op;
    }

    function UpdateAndReassign10<'T> (op : ('T => Unit is Ctl)) : Unit {
        mutable arr = new ('T => Unit is Adj + Ctl)[10];
        set arr w/= 0 <- op;
    }


    // apply-and-reassign statements

    function ApplyAndReassign1 () : Unit {
        mutable i = 0; 
        set i += 1; 
        set i -= 1;
        set i *= 10;
        set i /= 2;
        set i %= 3;
        set i ^= 2;
    }

    function ApplyAndReassign2 () : Unit {
        mutable i = true; 
        set i and= false;
        set i or= true;
    }

    function ApplyAndReassign3 () : Unit {
        mutable i = 23; 
        set i &&&= 2^10 - 1;
        set i |||= 1;
        set i ^^^= 2^10 - 1;
        set i <<<= 3;
        set i >>>= 1;
    }

    function ApplyAndReassign4 () : Unit {
        mutable i = 1L; 
        set i ^= 2;
    }

    function ApplyAndReassign5 () : Unit {
        mutable i = 1L; 
        set i += 2L ^ 2;
    }

    function ApplyAndReassign6 () : Unit {
        mutable i = true; 
        set i and= 1;
    }

    function ApplyAndReassign7 () : Unit {
        mutable i = 1; 
        set i += 1.;
    }

    function ApplyAndReassign8 () : Unit {
        let i = 1; 
        set i += 1;
    }

    function ApplyAndReassign9 () : Unit {
        mutable a = new Int[10]; 
        set a[0] = 1;
    }

    function ApplyAndReassign10 () : Unit {
        mutable a = new Int[10]; 
        set a[0] += 1;
    }


    // accessing named items in user defined types

    newtype OpPair = (Op1 : (Unit => Unit), Op2 : (Unit => Unit));
    newtype AdjWithArg = ((a1 : Qubit[], a2 : (Qubit[] => Unit is Ctl)), A1 : (((Qubit[] => Unit), Qubit[]) => Unit is Adj));
    newtype UnitaryWithArg = (U1 : (Int => Unit is Adj + Ctl), (param : Int));
    newtype OpPairArr = (Array : OpPair[]);


    function ItemAccess1<'T> (arg : 'T) : Unit {
        let _ = arg::Name;
    }

    function ItemAccess2 (arg : NamedItems7) : Unit {
        let _ = arg::NotAName;
    }

    function ItemAccess3 (arg1 : NamedItems1, arg2 : NamedItems7) : Int {
        return arg1::Re + arg1::Im + arg2::Name;
    }

    function ItemAccess4 (arg : NamedItems2) : Int {
        let (d, _) = arg!;
        return d < 0. ? 0 | arg::Re + arg::Im;
    }

    function ItemAccess5 (arg1 : NamedItems2, arg2 : NamedItems6) : (Int, Double) {
        let (d, (_, im)) = arg1!;
        let (((i1, i2), p), (c, (d1, d2))) = arg2!;
        if (p != arg2::Phase or c != arg2::Const) { fail "should be equal"; }
        return (d + arg1::Re + i1 - i2, d1 * d2);
    }

    function ItemAccess6 (arg1 : NamedItems4, arg2 : NamedItems5) : ((Double, Double), Double, Int, (Int, Int)) {
        let (_, pair1) = arg1!;
        let (pair2, _) = arg2!;
        return (pair1, arg1::Const, arg2::Phase, pair2);
    }

    function ItemAccess7 (arg1 : ArrayType4, arg2 : ArrayType5) : (Int, Int, Int)[] {
        if (Length(arg1::Name) != Length(arg2::Name)) {
            fail "length mismatch";
        }

        mutable res = new (Int,Int,Int)[0];
        for ((i1, i2) in arg2::Name) {
            set res += [(i1, i2, arg1::Name[Length(res)])];
        }
        return res;
    }

    function ItemAccess8 (arg : ArrayType8) : (Double, Double)[] {
        mutable ((_, res), i) = (arg!, 0);
        for ((fst, snd) in res) {
            set res w/= i <- (fst + arg :: Const, snd);
            set i += 1;
        }
        return res;
    }

    function ItemAccess9 (arg : ArrayType9) : (Int, Int)[] {
        mutable ((res, _), i) = (arg!, 0);
        for ((fst, snd) in res) {
            set res w/= i <- (fst + arg :: Phase, snd);
            set i += 1;
        }
        return res;
    }

    function ItemAccess10 (arg : ArrayType10) : Unit {
        if (Length(arg :: Phase) != Length(arg :: Const)) {
            fail "not the same length";
        }
    }

    operation ItemAccess11 (arg : OpPair) : Unit {
        arg::Op1 (arg :: Op2());
    }

    function ItemAccess12 (arg : OpPair) : Unit {
        arg::Op1 (arg :: Op2());
    }

    operation ItemAccess13 (arg : OpPair) : Unit
    is Adj {
        arg::Op1 ();
        arg :: Op2();
    }
        
    operation ItemAccess14 (arg : AdjWithArg) : Unit
    is Adj {
        arg::A1 (arg::a2, arg::a1);
    }

    operation ItemAccess15 (arg : AdjWithArg) : (((Qubit[] => Unit is Ctl), Qubit[]) => Unit is Adj) {
        return arg::A1;
    }

    operation ItemAccess16 (arg : UnitaryWithArg) : Unit 
    is Adj + Ctl {
        arg::U1 (arg::param);
    }

    operation ItemAccess17 (arg : OpPairArr) : Unit {
        arg::Array[0]::Op1();
    }

    operation ItemAccess18 (arg : OpPairArr[]) : Unit {
        arg[0]::Array[0]::Op1();
    }

    function ItemAccess19 (arg : OpPair) : OpPair[] {
        let arr = new OpPairArr[1];
        return arr[0]::Array;
    }

    function ItemAccess20 (arg : (Unit => Unit)) : OpPair {
        return (OpPairArr([OpPair(arg, arg)]))::Array[0];
    }


    function ItemUpdate1 (arg : NamedItems1) : Unit {
        mutable foo = arg w/ Re <- 1;
        set foo w/= Im <- 10;
    }

    function ItemUpdate2 (arg : NamedItems1) : Unit {
        mutable foo = arg w/ Re <- 1;
        set foo w/= Im <- "";
    }

    function ItemUpdate3 (arg : NamedItems1) : Unit {
        let _ = arg w/ Re <- (2,1);
    }

    function ItemUpdate4 (arg : NamedItems6) : Unit {
        mutable foo = arg 
            w/ Phase <- 1
            w/ Const <- 1.;
        set foo w/= Const <- 10.;
    }

    function ItemUpdate5 (arg : NamedItems6) : Unit {
        set arg w/= Const <- 10.;
    }

    function ItemUpdate6 (arg : NamedItems6) : Unit {
        mutable foo = arg 
            w/ Phase <- 1.
            w/ Const <- 1.;
    }

    function ItemUpdate7 (arg : NamedItems6) : Unit {
        mutable foo = arg 
            w/ Phase <- 1
            w/ Const <- "";
    }

    function ItemUpdate8 (arg : NamedItems6) : Unit {
        mutable foo = arg;
        set foo w/= Const <- "";
        set foo w/= Phase <- "";
    }

    function ItemUpdate9 () : Unit {
        mutable arr = new ArrayType10[5];
        for (i in 0..4) {
            mutable item = arr[i] w/ Phase <- new Int[10];
            set item w/= Const <- new Double[10];
            set arr w/= i <- item;
        }
    }

    function ItemUpdate10 () : Unit {
        mutable arr = new ArrayType10[5];
        for (i in 0..4) {
            mutable item = arr[i] w/ Phase <- new Int[10];
            set item::Phase w/= 0 <- 1;
        }
    }

    function ItemUpdate11 () : Unit {
        mutable arr = new ArrayType10[5];
        for (i in 0..4) {
            set arr[i] w/= Phase <- new Int[10];
        }
    }

    function ItemUpdate12 () : Unit {
        mutable arr = new ArrayType10[5];
        for (i in 0..4) {
            mutable item = arr[i] w/ Phase <- 1;
            set item w/= Const <- 10.;
            set arr w/= i <- item;
        }
    }

    operation ItemUpdate13 (
        op  : (Unit => Unit),
        adj : (Unit => Unit is Adj), 
        ctl : (Unit => Unit is Ctl)) 
    : Unit {
        mutable p1 = OpPair(adj, adj);
        set p1 w/= Op1 <- ctl;
        set p1 w/= Op1 <- op;
        let p2 = OpPair(ctl, ctl) 
            w/ Op1 <- op
            w/ Op2 <- adj;
    }

    operation ItemUpdate14 (
        unitary : (Int => Unit is Adj + Ctl),
        ctl        : (Int => Unit is Ctl)) 
    : Unit {
        mutable u1 = UnitaryWithArg(unitary, 0);
        set u1 w/= U1 <- ctl;
    }

    operation ItemUpdate15 (
        unitary : (Int => Unit is Adj + Ctl),
        adj        : (Int => Unit is Adj))
    : Unit {
        let _ = UnitaryWithArg(unitary, 0)
            w/ U1 <- adj;
    }

    operation ItemUpdate16 (unitary : (Unit => Unit is Adj + Ctl)) : Unit 
    is Adj + Ctl {
        let p = OpPair(unitary, unitary);
        p::Op1();
        p::Op2();
    }

    operation ItemUpdate17 (
        op        : (Unit => Unit),
        unitary : (Unit => Unit is Adj + Ctl)) 
    : Unit 
    is Adj + Ctl {
        mutable p = OpPair(op, op);
        set p w/= Op1 <- unitary;
        p::Op1();
    }

    operation ItemUpdate18 (
        op        : (Unit => Unit),
        unitary : (Unit => Unit is Adj + Ctl)) 
    : Unit 
    is Adj + Ctl {
        let p = OpPair(op, op) w/ Op1 <- unitary;
        p::Op1();
    }

    operation ItemUpdate19 (
        op        : (Unit => Unit),
        unitary : (Unit => Unit is Adj + Ctl)) 
    : Unit 
    is Adj + Ctl {
        (OpPair(op, op) w/ Op1 <- unitary)::Op1();
    }

    operation ItemUpdate20 (
        op        : (Unit => Unit),
        unitary : (Unit => Unit is Adj + Ctl)) 
    : Unit {
        (OpPair(op, op) w/ Op1 <- unitary)::Op1();
    }

    function ItemUpdate21 (arg : OpPair) : Unit {
        let arr = new OpPairArr[1];
        let _ = arr[0]::Array w/ 0 <- arg;
    }

    function ItemUpdate22 (arg : (Unit => Unit)) : Unit {
        let _ = (OpPairArr([OpPair(arg, arg)]))::Array[0] w/ Op1 <- arg;
    }


    // conjugations

    operation ValidConjugation1 () : Unit {
        mutable foo = 1;
        within {}
        apply {
            set foo = 10;
        }
    }

    operation ValidConjugation2 () : Unit {
        mutable foo = 1;
        mutable bar = -1;
        within {
            GenericAdjointable(bar);
        }
        apply {
            set foo = 10;
        }
    }


    operation ValidConjugation3 () : Unit {
        mutable foo = 1;
        within {}
        apply {
            set (_, foo) = (1, 10);
        }
    }

    operation ValidConjugation4 () : Unit {
        mutable foo = 1;
        mutable bar = -1;
        within {
            GenericAdjointable(bar);
        }
        apply {
            set (_, foo) = (1, 10);
        }
    }

    operation ValidConjugation5 () : Unit {
        mutable foo = 1;
        mutable bar = -1;
        within {
            let _ = bar;
        }
        apply {
            set (_, foo) = (1, 10);
        }
    }

    operation ValidConjugation6 () : Unit {
        mutable foo = 1;
        mutable bar = -1;
        within {
            let _ = bar;
        }
        apply {
            mutable nrIter = 0;
            repeat {
                set nrIter += 1;
                GenericOperation();
            }
            until (false);
        }
    }

    operation ValidConjugation7 (cond : Bool) : Unit {
        mutable foo = 1;
        within {
            if (cond) {
                fail "{foo}";
            } 
        }
        apply {
            if (not cond) {
                set (_, (foo, _)) = (1, (10, ""));
            }
        }
    }

    operation ValidConjugation8 (cond : Bool) : Unit {
        mutable foo = 1;
        within {
            for (i in 1 .. 10) {
                GenericAdjointable(i, (i, foo));
            } 
        }
        apply {
            repeat {}
            until (cond)
            fixup {}
        }
    }


    operation InvalidConjugation1 () : Unit {
        mutable foo = 1;
        within {
            GenericAdjointable(foo); 
        }
        apply {
            set foo = 10;
        }
    }

    operation InvalidConjugation2 () : Unit {
        mutable foo = 1;
        within {
            GenericAdjointable($"{foo}"); 
        }
        apply {
            set foo = 10;
        }
    }

    operation InvalidConjugation3 () : Unit {
        mutable foo = 1;
        within {
            let _ = foo; 
        }
        apply {
            set foo = 10;
        }
    }

    operation InvalidConjugation4 () : Unit {
        mutable foo = 1;
        within {
            let _ = foo; 
        }
        apply {
            set (_, foo) = (1, 10);
        }
    }

    operation InvalidConjugation5 () : Unit {
        mutable foo = 1;
        within {
            if (foo + 1 > 0) {} 
        }
        apply {
            set (_, foo) = (1, 10);
        }
    }

    operation InvalidConjugation6 (cond : Bool) : Unit {
        mutable foo = 1;
        within {
            if (cond) {
                fail $"{foo}";
            } 
        }
        apply {
            set (_, (foo, _)) = (1, (10, ""));
        }
    }

    operation InvalidConjugation7 (cond : Bool) : Unit {
        mutable foo = 1;
        within {
            if (cond) {
                fail $"{foo}";
            } 
        }
        apply {
            if (not cond) {
                set (_, (foo, _)) = (1, (10, ""));
            }
        }
    }

    operation InvalidConjugation8 (cond : Bool) : Unit {
        mutable foo = 1;
        within {
            for (i in 1 .. 10) {
                GenericAdjointable(i, (i, foo));
            } 
        }
        apply {
            repeat {}
            until (cond)
            fixup {
                set (_, (foo, _)) = (1, (10, ""));
            }
        }
    }

    
    // open-ended ranges in array slicing expressions

    function ValidArraySlice1 (arr : Int[]) : Int[] {
        return arr[3...];            
    } 

    function ValidArraySlice2 (arr : Int[]) : Int[] {
        return arr [0 .. 2 ... ];    
    } 

    function ValidArraySlice3 (arr : Int[]) : Int[] {
        return arr[...2];            
    } 

    function ValidArraySlice4 (arr : Int[]) : Int[] {
        return arr[...2..3];        
    } 

    function ValidArraySlice5 (arr : Int[]) : Int[] {
        return arr[...2...];        
    } 

    function ValidArraySlice6 (arr : Int[]) : Int[] {
        return arr[...];            
    } 

    function ValidArraySlice7 (arr : Int[]) : Int[] {
        return arr [4 .. -2 ... ];
    } 

    function ValidArraySlice8 (arr : Int[]) : Int[] {
        return arr[ ... -1 .. 3];    
    } 

    function ValidArraySlice9 (arr : Int[]) : Int[] {
        return arr[...-1...];        
    } 


    function InvalidArraySlice1 (arr : BigEndian) : Int[] {
        return arr[3...];            
    } 

    function InvalidArraySlice2 (arr : BigEndian) : Int[] {
        return arr [0 .. 2 ... ];    
    } 

    function InvalidArraySlice3 (arr : BigEndian) : Int[] {
        return arr[...2];            
    } 

    function InvalidArraySlice4 (arr : BigEndian) : Int[] {
        return arr[...2..3];        
    } 

    function InvalidArraySlice5 (arr : BigEndian) : Int[] {
        return arr[...2...];        
    } 

    function InvalidArraySlice6 (arr : BigEndian) : Int[] {
        return arr[...];            
    } 

    function InvalidArraySlice7 (arr : BigEndian) : Int[] {
        return arr [4 .. -2 ... ];
    } 

    function InvalidArraySlice8 (arr : BigEndian) : Int[] {
        return arr[ ... -1 .. 3];    
    } 

    function InvalidArraySlice9 (arr : BigEndian) : Int[] {
        return arr[...-1...];        
    } 


    // deprecation warnings

    @ Attribute()
    @ Deprecated("")
    newtype DeprecatedAttribute = Unit;

    @ Attribute()
    @ Deprecated("OldAttribute")
    newtype RenamedAttribute = Unit;

    @ Deprecated("")
    newtype DeprecatedType = Unit;

    @ Deprecated("NewTypeName")
    newtype RenamedType = Unit;


    newtype DeprecatedItemType = (Unit -> DeprecatedType)[];

    newtype RenamedItemType = (Int, RenamedType);

    function UsingDeprecatedConstructor1 () : Unit {
        let _ = DeprecatedType();
    }

    function UsingDeprecatedConstructor2 () : Unit {
        let _ = RenamedType();
    }


    @ DeprecatedAttribute()
    function UsingDeprecatedAttribute1 () : Unit {}

    @ DeprecatedAttribute()
    operation UsingDeprecatedAttribute2 () : Unit {}

    @ DeprecatedAttribute()
    newtype UsingDeprecatedAttribute3 = Unit;

    @ RenamedAttribute()
    function UsingRenamedAttribute1 () : Unit {}

    @ RenamedAttribute()
    operation UsingRenamedAttribute2 () : Unit {}

    @ RenamedAttribute()
    newtype UsingRenamedAttribute3 = Unit;


    function UsingDeprecatedType1 () : Unit {
        let _ = new DeprecatedType[0];
    }

    function UsingDeprecatedType2 (arg : DeprecatedType) : Unit {}

    function UsingDeprecatedType3 (arg : (DeprecatedType -> Unit)) : Unit {}

    function UsingDeprecatedType4 () : DeprecatedType {
        return Default<DeprecatedType>();
    }

    function UsingDeprecatedType5 () : (DeprecatedType[], Int) {
        return Default<(DeprecatedType[], Int)>();
    }


    function UsingRenamedType1 () : Unit {
        let _ = new RenamedType[0];
    }

    function UsingRenamedType2 (arg : RenamedType) : Unit {}

    function UsingRenamedType3 (arg : (RenamedType -> Unit)) : Unit {}

    function UsingRenamedType4 () : RenamedType {
        return Default<RenamedType>();
    }

    function UsingRenamedType5 () : (RenamedType[], Int) {
        return Default<(RenamedType[], Int)>();
    }

    // TODO: CHECK USING DEPRECATED STUFF AS ATTRIBUTES AND IN UNDERLYING TYPES OF USER DEFINED TYPES

}