// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for type checking
namespace Microsoft.Quantum.Testing.TypeChecking {

    open Microsoft.Quantum.Testing.General;


    // utils for testing variance behavior 

    function TakesBigEndian (a : BigEndian) : Unit {}
    function TakesOpArray<'T> (a : ('T => Unit)[]) : Unit {}
    function TakesOpArrayFunction<'T> (fct : (('T => Unit)[] -> Unit)) : Unit {}
    function TakesOpFunction<'T> (fct : (('T => Unit) -> Unit)) : Unit {}
    function TakesOpFctFunction<'T> (fct : ((('T => Unit) -> Unit) -> Unit)) : Unit {}
    function TakesOpFctFctFunction<'T> (fct : (((('T => Unit) -> Unit) -> Unit) -> Unit)) : Unit {}
    function TakesAdjArray<'T> (a : ('T => Unit is Adj)[]) : Unit {}
    function TakesAdjArrayFunction<'T> (fct : (('T => Unit is Adj)[] -> Unit)) : Unit {}
    function TakesAdjFunction<'T> (fct : (('T => Unit is Adj) -> Unit)) : Unit {}
    function TakesAdjFctFunction<'T> (fct : ((('T => Unit is Adj) -> Unit) -> Unit)) : Unit {}
    function TakesAdjFctFctFunction<'T> (fct : (((('T => Unit is Adj) -> Unit) -> Unit) -> Unit)) : Unit {}
    function TakesUnitaryArray<'T> (a : ('T => Unit is Adj + Ctl)[]) : Unit {}
    function TakesAnyArray<'T> (a : 'T[]) : Unit {}    
    function TakesIntFunction (fct : (Int -> Unit)) : Unit {}
    function TakesAnyFunction<'T> (fct : ('T -> Unit)) : Unit {}
    function TakesAnyArrayFunction<'T> (fct : ('T[] -> Unit)) : Unit {}
    function TakesIntOperation (op : (Int => Unit)) : Unit {}
    function TakesAnyOperation<'T> (op : ('T => Unit)) : Unit {}
    function TakesAnyAdjointable<'T> (op : ('T => Unit is Adj)) : Unit {}
    function TakesAnyUnitary<'T> (op : ('T => Unit is Adj + Ctl)) : Unit {}
    
    
    // variance behavior 
    
    function Variance1 () : Unit {
        let opArray = new (Qubit => Unit)[0];
        let adjArray = new (Qubit => Unit is Adj)[0];
        let unitaryArray = new (Qubit => Unit is Adj + Ctl)[0];

        TakesBigEndian(BigEndian(new Qubit[0]));
        TakesOpArray(opArray);
        TakesAdjArray(adjArray);
        TakesUnitaryArray(unitaryArray);
        TakesAnyArray(new Qubit[0]);
        TakesAnyArray(opArray);
        TakesAnyArray(adjArray);
        TakesAnyArray(unitaryArray); 
    }

    function Variance2 () : Unit {
        TakesBigEndian(new Qubit[0]); 
    }

    function Variance3 () : Unit {
        TakesAnyArray(BigEndian(new Qubit[0])); 
    }

    function Variance4 () : Unit {
        let adjArray = new (Qubit => Unit is Adj)[0];
        TakesOpArray(adjArray);
    }

    function Variance5 () : Unit {
        let unitaryArray = new (Qubit => Unit is Adj + Ctl)[0];
        TakesAdjArray(unitaryArray);
    }

    function Variance6 () : Unit {
        let adjArray = new (Qubit => Unit is Adj)[0];
        TakesUnitaryArray(adjArray);
    }

    function Variance7 () : Unit {
        TakesAnyFunction(TakesBigEndian);
        TakesAnyFunction(TakesOpArray);
        TakesAnyFunction(TakesUnitaryArray);
        TakesAnyFunction(TakesAnyArray);
        TakesAnyFunction(TakesIntFunction);
        TakesAnyFunction(TakesAnyFunction);
        TakesAnyFunction(TakesAnyArrayFunction);
        TakesAnyFunction(TakesAnyOperation);
        TakesAnyFunction(TakesAnyUnitary);
    }

    function Variance8 () : Unit {
        TakesAnyArrayFunction(TakesAnyArray);
        TakesOpArrayFunction(TakesOpArray); 
        TakesAdjArrayFunction(TakesAdjArray); 

        TakesOpFunction(TakesAnyOperation); 
        TakesAdjFunction(TakesAnyAdjointable); 
        TakesAdjFunction(TakesAnyOperation); 

        TakesOpFctFunction(TakesOpFunction); 
        TakesAdjFctFunction(TakesAdjFunction); 
        TakesOpFctFunction(TakesAdjFunction); 

        TakesOpFctFctFunction(TakesOpFctFunction); 
        TakesAdjFctFctFunction(TakesAdjFctFunction);
        TakesAdjFctFctFunction(TakesOpFctFunction);
    }

    function Variance9 () : Unit {
        TakesAnyArrayFunction(TakesBigEndian);
    }

    function Variance10 () : Unit {
        TakesOpArrayFunction(TakesAdjArray);
    }

    function Variance11 () : Unit {
        TakesAdjArrayFunction(TakesOpArray); 
    }

    function Variance12 () : Unit {
        TakesOpFunction(TakesAnyAdjointable); 
    }

    function Variance13 () : Unit {
        TakesAdjFctFunction(TakesOpFunction); 
    }

    function Variance14 () : Unit {
        TakesOpFctFctFunction(TakesAdjFctFunction); 
    }


    // utils for determining common base type

    newtype OpTuple = ((Unit => Unit), (Unit => Unit)); 
    newtype AdjCtlTuple = ((Unit => Unit is Adj), (Unit => Unit is Ctl)); 

    operation SelfAdjOp () : Unit {
        adjoint self;
    }

    operation IntrinsicAdj () : Unit 
    is Adj {
        body intrinsic;
    }


    // determine common base type

    function CommonBaseType1 () : (Unit => Unit)[] {
        let arr1 = new (Unit => Unit)[0]; 
        let arr2 = new (Unit => Unit)[0]; 
        return arr1 + arr2;
    }

    function CommonBaseType2 () : (Unit => Unit)[] {
        let arr1 = new (Unit => Unit is Adj)[0]; 
        let arr2 = new (Unit => Unit)[0]; 
        return arr1 + arr2;
    }

    function CommonBaseType3 () : (Unit => Unit)[] {
        let arr1 = new (Unit => Unit is Adj)[0]; 
        let arr2 = new (Unit => Unit is Adj + Ctl)[0]; 
        return arr1 + arr2;
    }

    function CommonBaseType4 () : (Unit => Unit)[] {
        let arr1 = new (Unit => Unit is Adj)[0]; 
        let arr2 = new (Unit => Unit is Adj)[0]; 
        return arr1 + arr2;
    }

    function CommonBaseType5 (
        op1 : (Unit => Unit), 
        op2 : (Unit => Unit)) 
    : (Unit => Unit)[] {
        return [op1, op2];
    }

    function CommonBaseType6 (
        op1 : (Unit => Unit is Adj), 
        op2 : (Unit => Unit)) 
    : (Unit => Unit)[] {
        return [op1, op2];
    }

    function CommonBaseType7 (
        op1 : (Unit => Unit is Adj), 
        op2 : (Unit => Unit is Adj + Ctl)) 
    : (Unit => Unit is Adj)[] {
        return [op1, op2];
    }

    function CommonBaseType8 (
        op1 : (Unit => Unit is Adj), 
        op2 : (Unit => Unit is Adj + Ctl)) 
    : (Unit => Unit)[] {
        return [op1, op2];
    }

    function CommonBaseType9 (
        op1 : (Unit => Unit), 
        op2 : (Unit => Unit is Adj), 
        op3 : (Unit => Unit is Adj + Ctl)) 
    : (Unit => Unit)[] {
        mutable res = [op1, op2, op3];
        set res = [op2, op3, op1];
        set res = [op3, op1, op2];
        set res = [op1, op3, op2]; 
        set res = [op3, op2, op1]; 
        set res = [op2, op1, op3]; 
        set res = [op2, op3, op1]; 
        return res;
    }

    function CommonBaseType10 (
        op1 : (Unit => Unit is Adj + Ctl),
        op2 : (Unit => Unit is Ctl + Adj)) 
    : (Unit => Unit is Ctl + Adj)[] {
        mutable arr = [op1];
        set arr += [op2];
        return arr;
    }

    function CommonBaseType11 () : BigInt {
        return 1 + 1; 
    }

    function CommonBaseType12 () : BigInt {
        return 1L + 1; 
    }

    function CommonBaseType13 (arg1 : OpTuple, arg2 : OpTuple) : OpTuple[] {
        return [arg1, arg2]; 
    }

    function CommonBaseType14 (arg1 : OpTuple, arg2 : AdjCtlTuple) : ((Unit => Unit), (Unit => Unit))[] {
        return [arg1!, arg2!]; 
    }

    function CommonBaseType15 (arg1 : OpTuple, arg2 : AdjCtlTuple) : OpTuple[] {
        return [arg1!, arg2!]; 
    }

    function CommonBaseType16 (arg1 : OpTuple, arg2 : AdjCtlTuple) : OpTuple[] {
        return [arg1, arg2]; 
    }

    function CommonBaseType17 () : (Unit => Unit is Adj)[] {
        return [SelfAdjOp, IntrinsicAdj];
    }

    function CommonBaseType18<'A> (a1 : 'A, a2 : 'A) : 'A[] {
        return [a1, a2];
    }

    function CommonBaseType19<'A,'B> () : 'A[] {
        return [(new 'A[1])[0], (new 'A[1])[0]];
    }

    function CommonBaseType20<'A,'B> (a : 'A, b : 'B) : 'A[] {
        return [(new 'A[1])[0], (new 'B[1])[0]];
    }

    function CommonBaseType21 () : BigEndian[] {
        return [(new BigEndian[1])[0], (new BigEndian[1])[0]];
    }

    function CommonBaseType22 () : (Int -> Unit)[] {
        return [GenericFunction<Int>, GenericFunction<Int>];
    }

    function CommonBaseType23 () : (Int -> Unit)[] {
        let fct = GenericFunction<Int>;
        return [GenericFunction<Int>, fct];
    }

    function CommonBaseType24 () : (Int -> Unit)[] {
        let fct = GenericFunction<Int>;
        return [fct, fct];
    }

    function CommonBaseType25 () : (Int -> Unit)[] {
        return [GenericFunction<Int>, GenericFunction<Double>];
    }


    // Equality comparison

    function UnitEquality(x : Unit, y : Unit) : Bool { return x == y; }
    function UnitInequality(x : Unit, y : Unit) : Bool { return x != y; }
    function IntEquality(x : Int, y : Int) : Bool { return x == y; }
    function IntInequality(x : Int, y : Int) : Bool { return x != y; }
    function BigIntEquality(x : BigInt, y : BigInt) : Bool { return x == y; }
    function BigIntInequality(x : BigInt, y : BigInt) : Bool { return x != y; }
    function DoubleEquality(x : Double, y : Double) : Bool { return x == y; }
    function DoubleInequality(x : Double, y : Double) : Bool { return x != y; }
    function BoolEquality(x : Bool, y : Bool) : Bool { return x == y; }
    function BoolInequality(x : Bool, y : Bool) : Bool { return true != true; }
    function StringEquality(x : String, y : String) : Bool { return x == y; }
    function StringInequality(x : String, y : String) : Bool { return x != y; }
    function QubitEquality(x : Qubit, y : Qubit) : Bool { return x == y; }
    function QubitInequality(x : Qubit, y : Qubit) : Bool { return x != y; }
    function ResultEquality(x : Result, y : Result) : Bool { return x == y; }
    function ResultInequality(x : Result, y : Result) : Bool { return x != y; }
    function PauliEquality(x : Pauli, y : Pauli) : Bool { return x == y; }
    function PauliInequality(x : Pauli, y : Pauli) : Bool { return x != y; }
    function RangeEquality(x : Range, y : Range) : Bool { return x == y; }
    function RangeInequality(x : Range, y : Range) : Bool { return x != y; }
    function ArrayEquality(x : Int[], y : Int[]) : Bool { return x == y; }
    function ArrayInequality(x : Int[], y : Int[]) : Bool { return x != y; }
    function TupleEquality(x : (Int, Int), y : (Int, Int)) : Bool { return x == y; }
    function TupleInequality(x : (Int, Int), y : (Int, Int)) : Bool { return x != y; }
    function UDTEquality(x : NamedItems1, y : NamedItems1) : Bool { return x == y; }
    function UDTInequality(x : NamedItems1, y : NamedItems1) : Bool { return x != y; }
    function GenericEquality<'A>(x : 'A, y : 'A) : Bool { return x == y; }
    function GenericInequality<'A>(x : 'A, y : 'A) : Bool { return x != y; }
    function OperationEquality(x : (Unit => Unit), y : (Unit => Unit)) : Bool { return x == y; }
    function OperationInequality(x : (Unit => Unit), y : (Unit => Unit)) : Bool { return x != y; }
    function FunctionEquality(x : (Unit -> Unit), y : (Unit -> Unit)) : Bool { return x == y; }
    function FunctionInequality(x : (Unit -> Unit), y : (Unit -> Unit)) : Bool { return x != y; }
    function InvalidTypeEquality(x : __Invalid__, y : __Invalid__) : Bool { return x == y; }
    function InvalidTypeInequality(x : __Invalid__, y : __Invalid__) : Bool { return x != y; }
    function NoCommonBaseEquality(x : Int, y : String) : Bool { return x == y; }
    function NoCommonBaseInequality(x : Int, y : String) : Bool { return x != y; }


    // utils for testing type matching of arguments

    function GenSimple<'A> (arg : 'A) : 'A {
        return arg;
    }

    function GenSimpleArray<'A> (arg : 'A[]) : 'A {
        return arg[0]; 
    }

    function GenPairOfSame<'A> (a1 : 'A, a2 : 'A) : 'A {
        return true ? a1 | a2;
    }

    function GenPair<'A,'B> (a : 'A, b : 'B) : ('A, 'B) {
        return (a,b);
    }


    // type matching of arguments

    function MatchArgument1 () : (Int, Double, String) {
        return (GenSimple(1), GenSimple(1.), GenSimple(""));
    }

    function MatchArgument2 () : (Int, Double, String) {
        return GenSimple(1, 1., "");
    }

    function MatchArgument3<'T> (a : 'T) : ('T[], Int[]) {
        return GenSimple(new 'T[0], new Int[0]);
    }

    function MatchArgument4 () : Unit {
        return GenSimple();
    }

    function MatchArgument5 () : Int {
        return GenSimpleArray([1,2,3]); 
    } 

    function MatchArgument6 () : Int[] {
        return GenSimpleArray([[1,2,3]]); 
    } 

    function MatchArgument7<'T> (a : 'T[]) : 'T {
        return GenSimpleArray(a); 
    } 

    function MatchArgument8 (op : (Unit => Unit is Ctl)) : (Unit => Unit) {
        return GenSimpleArray([op]); 
    } 

    function MatchArgument9<'T> (a : 'T) : Unit {
        let _ = GenSimpleArray(a); 
    } 

    function MatchArgument10 () : (Int, String) {
        return GenPairOfSame(1,"");
    } 

    function MatchArgument11 () : String {
        return GenPairOfSame("","");
    } 

    function MatchArgument12<'T> (a : 'T) : 'T {
        let b = GenSimple(a); 
        return GenPairOfSame(a,b);
    } 

    function MatchArgument13 () : (Int, Double) {
        return GenPair(1,1.);
    } 

    function MatchArgument14 () : (Int, Double) {
        let pair = (1, 1.);
        return GenPair(pair);
    } 

    function MatchArgument15 () : ((Int, Double), String) {
        return GenPair((1, 1.), "");
    } 

    function MatchArgument16 () : ((Int, Unit), String) {
        return GenPair((1, ()), "");
    } 

    function MatchArgument17 () : (Int, (Unit, String)) {
        return GenPair(1, ((), ""));
    } 

    function MatchArgument18 () : (Double, Double) {
        return GenPairOfSame((GenPairOfSame(1.,2.), GenPairOfSame(1.,2.)), GenPair(1.,2.));
    } 

    function MatchArgument19 () : Unit {
        let _ = GenPair(1, 1., "");
    } 


    // utils for testing partial application

    function TakesTriplet(a : Int, b : Double, c : Result) : Unit {}
    function TakesNestedTriplet (a : (Int, Int, Int), b : ((Double, Double), (Result, Result, Result))) : Unit  {}
    operation TakesTripletOp(a : Int, b : Double, c : Result) : Unit {}
    operation TakesTripletAdj(a : Int, b : Double, c : Result) : Unit is Adj {}
    operation TakesTripletCtl(a : Int, b : Double, c : Result) : Unit is Ctl {}


    // partial application

    function PartialApplication1 () : Unit {
        let f3 = TakesTriplet; 
        let f2 = f3(_,_,Zero); 
        let f1 = f2(1,_); 
        f1(1.); 
    }

    function PartialApplication2 () : Unit {
        let _ = TakesTriplet(_,Zero); 
    }

    function PartialApplication3 () : Unit {
        let f2 = TakesTriplet(_,1.,_);
        let _ = f2 (_, 1); 
    }

    function PartialApplication4 () : Unit {
        let f2 = TakesTriplet(_,1.,_);
        let _ = f2 ("",_); 
    }

    function PartialApplication5 () : Unit {
        let f2 = TakesTriplet(_,1.,_);
        let f1 = f2 (1,_);
        f1(""); 
    }
    
    function PartialApplication6 () : Unit {
        let f1 = TakesNestedTriplet ((1,1,1),_);
        let f2 = f1((1.,1.), _);
        f2(Zero,Zero,Zero);
    }
    
    function PartialApplication7 () : Unit {
        let f1 = TakesNestedTriplet ((1,1,1),(1.,1.), _);
        f1(Zero,Zero,Zero);    
    }
    
    function PartialApplication8 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),((1.,1.), _));
        f1(1,Zero,Zero,Zero);
    }
    
    function PartialApplication9 () : Unit {
        let f1 = TakesNestedTriplet ((1,1,1),((1.,1.), _));
        f1(Zero,Zero,Zero);    
    }
    
    function PartialApplication10 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),((1.,1.), _));
        f1(1,(Zero,Zero,Zero));    
    }
    
    function PartialApplication11 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),((_,1.), (Zero,_,Zero)));
        f1(1,(1.,Zero));    
    }
    
    function PartialApplication12 () : Unit {
        (TakesNestedTriplet ((1,_,1),((_,1.), (Zero,_,Zero)))) (1,(1.,Zero));    
    }
    
    function PartialApplication13 () : Unit {
        ((TakesNestedTriplet ((1,_,1),((_,1.), (Zero,_,Zero)))) (1,(_,Zero)))(1.);    
    }
    
    function PartialApplication14 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),(_, (Zero,_,Zero)));
        f1(1,((1.,1.),Zero));    
    }
    
    function PartialApplication15 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),((_,1.), (Zero,_,Zero)));
        f1(1.,(1,Zero));    
    }
    
    function PartialApplication16 () : Unit {
        let f1 = TakesNestedTriplet ((1,_,1),(_, (Zero,_,Zero)));
        f1(1,(1.,Zero));    
    }
    
    function PartialApplication17 () : Unit {
        (TakesTriplet(_,_,Zero))(1,1.);
    }
    
    function PartialApplication18 () : Unit {
        (TakesTripletOp(_,_,Zero))(1,1.);    
    }

    operation PartialApplication19 () : Unit {
        (TakesTripletOp(_,_,Zero))(1,1.);
    }

    operation PartialApplication20 () : Unit is Adj {
        let _ = TakesTripletOp(_,_,Zero);
    }

    operation PartialApplication21 () : Unit is Adj {
        (TakesTriplet(_,_,Zero))(1,1.);
    }

    operation PartialApplication22 () : Unit is Adj {
        (TakesTripletOp(_,_,Zero))(1,1.);
    }

    operation PartialApplication23 (qs : Qubit[]) : Unit {
        Adjoint (TakesTripletAdj(_,1.,_))(1,Zero);
        let adj = TakesTripletAdj(_,1.,_);
        Adjoint adj(1,Zero);

        Controlled (TakesTripletCtl(_,1.,_))(qs, (1,Zero));
        let ctl = TakesTripletCtl(_,1.,_);
        Controlled ctl(qs, (1,Zero));
    }

    operation PartialApplication24 (qs : Qubit[]) : Unit {
        let adj = TakesTripletAdj(_,1.,_);
        Controlled adj(qs, 1,Zero);
    }

    operation PartialApplication25 (qs : Qubit[]) : Unit {
        Controlled (TakesTripletAdj(_,1.,_))(qs, 1,Zero);
    }
    
    operation PartialApplication26 (qs : Qubit[]) : Unit {
        let ctl = TakesTripletCtl(_,1.,_);
        Adjoint ctl(qs, 1,Zero);
    }

    operation PartialApplication27 (qs : Qubit[]) : Unit {
        Adjoint (TakesTripletCtl(_,1.,_))(qs, 1,Zero);
    }

    function PartialApplication28 () : (Double -> Unit) {
        return TakesTriplet(_,1.,Zero);
    }

    function PartialApplication29 () : (Int -> Unit) {
        return TakesTriplet(_,1.,Zero);
    }

    function PartialApplication30 () : Unit {
        (PartialApplication29())(1); 
        let fct = PartialApplication29();
        fct(1);
    }

    // Sized array constructors

    function SizedArray1(n : Int) : Int[] {
        return [10, size = n];
    }

    function SizedArray2() : String[] {
        return ["foo", size = -1];
    }

    function SizedArray3<'a>(value : 'a) : 'a[] {
        return [value, size = 5];
    }

    function SizedArrayInvalid1(n : Double) : Int[] {
        return [10, size = n];
    }

    function SizedArrayInvalid2(n : String) : Int[] {
        return [10, size = n];
    }

    function SizedArrayInvalid3() : Int[] {
        return [5, size = (1, 2)];
    }
}
