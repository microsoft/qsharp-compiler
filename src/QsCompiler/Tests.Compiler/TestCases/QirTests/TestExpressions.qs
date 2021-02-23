// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    newtype TestType = ((P : Pauli, I : Int), Double);

    // This test makes sure that we properly push a value 
    // on the value stack for each expression.
    @EntryPoint()
    operation TestExpressions () : Unit{
        let _ = ReturnGlobalId();
        let _ = ReturnLocalId();
        let _ = ReturnFunctionCall();
        let _ = ReturnOperationCall();
        let _ = ReturnPartialApplication();
        let _ = ReturnUnwrapApplication();
        let _ = ReturnAdjointApplication();
        let _ = ReturnControlledApplication();
        let _ = ReturnTuple();
        let _ = ReturnArrayItem();
        let _ = ReturnNamedItem();
        let _ = ReturnArray();
        let _ = ReturnNewArray();
        let _ = ReturnString();
        let _ = ReturnRange();
        let _ = ReturnCopyAndUpdateArray();
        let _ = ReturnCopyAndUpdateUdt();
        let _ = ReturnConditional(1==0);
        let _ = ReturnEquality(4,5);
        let _ = ReturnInequality(5,6);
        let _ = ReturnLessThan(5,6);
        let _ = ReturnLessThanOrEqual(5,6);
        let _ = ReturnGreaterThan(5,6);
        let _ = ReturnGreaterThanOrEqual(5,6);
        let _ = ReturnLogicalAnd(true, false);
        let _ = ReturnLogicalOr(false, true);
        let _ = ReturnAddition(1., 2.);
        let _ = ReturnSubtraction(3, 4);
        let _ = ReturnMultiplication(1., 2.);
        let _ = ReturnDivision(3, 4);
        let _ = ReturnExponentiation1(5);
        let _ = ReturnExponentiation2(6.);
        let _ = ReturnExponentiation3(7);
        let _ = ReturnModulo(8);
        let _ = ReturnPauli();
        let _ = ReturnResult();
        let _ = ReturnBool();
        let _ = ReturnInt();
        let _ = ReturnDouble();
        let _ = ReturnBigInt();
        let _ = ReturnUnit();
        let _ = ReturnLeftShift();
        let _ = ReturnRightShift();
        let _ = ReturnBXOr();
        let _ = ReturnBOr();
        let _ = ReturnBAnd();
        let _ = ReturnNot();
        let _ = ReturnBNot();
        let _ = ReturnNegative(3.);
    }

    function ReturnGlobalId() : (Unit => Unit) {
        return TestExpressions;
    }

    operation ReturnLocalId() : (Unit -> (Unit => Unit)) {
        let fct = ReturnGlobalId;
        return fct;
    }

    operation ReturnFunctionCall() : Int {
        return ReturnInt();
    }

    operation ReturnOperationCall() : Double {
        return ReturnDouble();
    }

    function ReturnPartialApplication() : (Pauli -> TestType) {
        return (TestType((_, 2), _))(_, 2.);
    }

    function ReturnUnwrapApplication() : ((Pauli, Int), Double) {
        let test = TestType((PauliX, 3), 3.);
        return test!;
    }

    function ReturnAdjointApplication() : (Unit => Unit is Adj) {
        return Adjoint Unitary;
    }

    function ReturnControlledApplication() : ((Qubit[], Unit) => Unit is Ctl) {
        return Controlled Unitary;
    }

    function ReturnTuple() : (Int, ((Unit => Unit), String)) {
        return (1, (Unitary, ""));
    }

    function ReturnNamedItem() : Pauli {
        let test = TestType((PauliX, 4), 4.);
        return test::P;    
    }

    function ReturnArray() : String[] {
        return ["1", "2"];
    }

    function ReturnArrayItem() : String {
        let arr = ["3", "4"];
        return arr[1];
    }

    function ReturnNewArray() : Result[] {
        return new Result[5];
    }

    function ReturnString() : String {
        return "Hello";
    }

    function ReturnRange() : Range {
        return 10..-1..0;
    }

    function ReturnCopyAndUpdateArray() : Result[] {
        let arr = new Result[5];
        return arr w/ 3 <- One;
    }

    function ReturnCopyAndUpdateUdt() : TestType {
        return TestType((PauliI, 5), 5.) w/ P <- PauliX;
    }

    function ReturnConditional(arg : Bool) : BigInt {
        return arg ? 0L | 1L;
    }

    function ReturnEquality(a1 : Int, a2 : Int) : Bool {
        return a1 == a2;
    }

    function ReturnInequality(a1 : Int, a2 : Int) : Bool {
        let arg = a1 == 5 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return a1 != a2;
    }

    function ReturnLessThan(a1 : Int, a2 : Int) : Bool {
        let arg = a1 == 5 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return a1 < a2;
    }

    function ReturnLessThanOrEqual(a1 : Int, a2 : Int) : Bool {
        let arg = a1 == 5 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return a1 <= a2;
    }

    function ReturnGreaterThan(a1 : Int, a2 : Int) : Bool {
        let arg = a1 == 5 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return a1 > a2;
    }

    function ReturnGreaterThanOrEqual(a1 : Int, a2 : Int) : Bool {
        let arg = a1 == 5 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return a1 >= a2;
    }

    function ReturnLogicalAnd(b1 : Bool, b2 : Bool) : Bool {
        let arg = b1 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return b1 and b2;    
    }

    function ReturnLogicalOr(b1 : Bool, b2 : Bool) : Bool {
        let arg = b2 ? 10 | 11;
        let _ = ReturnConditional(arg == 12);
        return b1 or b2;    
    }

    function ReturnAddition(i1 : Double, i2 : Double) : Double {
        return i1 + i2;
    }

    function ReturnSubtraction(i1 : Int, i2 : Int) : Int {
        return i1 - i2;
    }

    function ReturnMultiplication(i1 : Double, i2 : Double) : Double {
        return i1 * i2;
    }

    function ReturnDivision(i1 : Int, i2 : Int) : Int {
        return i1 / i2;
    }

    function ReturnModulo(ex : Int) : Int {
        return 10 % ex;
    }

    function ReturnExponentiation1(ex : Int) : Int {
        return 4 ^ ex;
    }

    function ReturnExponentiation2(ex : Double) : Double {
        return 4. ^ ex;
    }

    function ReturnExponentiation3(ex : Int) : BigInt {
        return 4L ^ ex;
    }

    function ReturnLeftShift() : Int {
        return 22 <<< 2;
    }

    function ReturnRightShift() : BigInt {
        return 22L >>> 2;
    }

    function ReturnBXOr() : Int {
        return 42 ^^^ 4;
    }

    function ReturnBAnd() : BigInt {
        return 3L &&& 10L;
    }

    function ReturnBOr() : Int {
        return 33 ||| 31;
    }

    function ReturnBNot() : Int {
        return ~~~10;
    }

    function ReturnNot() : Bool {
        return not false;
    }

    function ReturnNegative(arg : Double) : Double {
        return -arg;
    }

    function ReturnPauli() : Pauli {
        return PauliZ;
    }
    
    function ReturnResult() : Result {
        return Zero;
    }
    
    function ReturnBool() : Bool {
        return false;
    }
    
    function ReturnInt() : Int {
        return 11;
    }
    
    function ReturnDouble() : Double {
        return 12.;
    }
    
    function ReturnBigInt() : BigInt {
        return 13L;
    }

    operation ReturnUnit() : Unit {
        return ();
    }

    operation Unitary() : Unit is Adj + Ctl {
    }
}
