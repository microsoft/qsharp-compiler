// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict1 {
    newtype Dummy = Unit;
}

/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict2 {
    newtype Dummy = Unit;
}

/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict3 {
    newtype Dummy = Unit;
}


/// This namespace contains test cases for syntax tree verification
namespace Microsoft.Quantum.Testing.GlobalVerification {

    open Microsoft.Quantum.Testing.Attributes;
    open Microsoft.Quantum.Testing.General;
    open Microsoft.Quantum.Testing.GlobalVerification.N3;
    open Microsoft.Quantum.Testing.GlobalVerification.N4 as N4;


    // types used to test cycle checking for user defined types across files

    newtype Qubits = Register; 
    newtype UnitType = UnitType; 
    newtype IntType = Microsoft.Quantum.Testing.GlobalVerification.N2.IntType;


    // local namespace short names

    function LocalNamespaceShortNames1 (
        arg : Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair) 
    : Unit {}

    function LocalNamespaceShortNames2 (
        arg : N4.IntPair) 
    : Unit {}

    function LocalNamespaceShortNames3 (arg : IntPair) 
    : Unit {}

    function LocalNamespaceShortNames4 () : Unit {
        let _ = new IntPair[5]; 
    }

    function LocalNamespaceShortNames5 () : Unit {
        let _ = IntPair(1, 1); 
    }

    function LocalNamespaceShortNames6 () : Unit {
        let _ = N4.IntPair(1, 1); 
    }

    function LocalNamespaceShortNames7 () : Unit {
        let n2Int = Microsoft.Quantum.Testing.GlobalVerification.N2.IntType(1);
        let n3Int = Microsoft.Quantum.Testing.GlobalVerification.N3.IntType(1);
        let _ = IntPair(n2Int, n3Int); 
    }

    function LocalNamespaceShortNames8 (arg : N4.IntPair) 
    : Unit {}

    function LocalNamespaceShortNames9 () : Unit {
        let _ = new N4.IntPair[5]; 
    }

    function LocalNamespaceShortNames10 () : Unit {
        let n2Int = Microsoft.Quantum.Testing.GlobalVerification.N2.IntType(1);
        let n3Int = Microsoft.Quantum.Testing.GlobalVerification.N3.IntType(1);
        let _ = N4.IntPair(n2Int, n3Int); 
    }

    function LocalNamespaceShortNames11 () : (IntPair -> Unit) {
        return TakesAnyArg;
    }

    function LocalNamespaceShortNames12 () : (N4.IntPair -> Unit) {
        return TakesAnyArg;
    }

    function LocalNamespaceShortNames13 () : (IntPair -> Unit) {
        return N4.TakesAnyArg;
    }

    function LocalNamespaceShortNames14 () : (N4.IntPair -> Unit) {
        return N4.TakesAnyArg;
    }

    function LocalNamespaceShortNames15 () : (N4.IntPair -> Unit) {
        return N4.TakesAnyArg<N4.IntPair>;
    }

    function LocalNamespaceShortNames16 () : (Unit -> IntPair) {
        return N4.Default<IntPair>;
    }

    function LocalNamespaceShortNames17 () : (Unit -> IntPair) {
        return N4.Default<N4.IntPair>;
    }

    function LocalNamespaceShortNames18 () : (Unit -> N4.IntPair) {
        return N4.Default<IntPair>;
    }

    function LocalNamespaceShortNames19 () : (Unit -> N4.IntPair) {
        return N4.Default<N4.IntPair>;
    }

    function LocalNamespaceShortNames20 () : Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair {
        return N4.Default<N4.IntPair>();
    }

    function LocalNamespaceShortNames21 () : N4.IntPair {
        return N4.Default<Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair>();
    }

    function LocalNamespaceShortNames22 () : Unit {
        let arr1 = new N4.IntPair[5]; 
        let arr2 = new Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair[5];
        let _ = arr1 + arr2;
    }

    function LocalNamespaceShortNames23 (arr1 : Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair[], arr2 : N4.IntPair[]) : Unit {
        let _ = arr1 + arr2;
    }

    newtype Udt1 = Microsoft.Quantum.Testing.GlobalVerification.N4.IntPair[];
    newtype Udt2 = N4.IntPair[];

    function LocalNamespaceShortNames24 (arg1 : Udt1, arg2 : Udt2) : Unit {
        let _ = arg1! + arg2!;
    }


    // naming conflicts with namespace name

    function NamingConflict1 () : Unit {}
    operation NamingConflict2 () : Unit {}
    newtype NamingConflict3 = Unit;
    function NamingConflict4 () : Unit {}
    operation NamingConflict5 () : Unit {}
    newtype NamingConflict6 = Unit;


    // checking for duplicate specializations

    operation ValidSetOfSpecializations1 () : Unit {
        body (...) {}
        adjoint auto; 
        controlled auto;
        adjoint controlled auto;
    }

    operation ValidSetOfSpecializations2 () : Unit {
        body (...) {}
        adjoint auto; 
        controlled auto;
        controlled adjoint auto;
    }

    operation ValidSetOfSpecializations3 () : Unit {
        body (...) {}
        controlled adjoint auto;
    }

    operation ValidSetOfSpecializations4 () : Unit {
        body (...) {}
        adjoint auto;
    }

    operation ValidSetOfSpecializations5 () : Unit {
        body (...) {}
        controlled auto;
    }

    operation ValidSetOfSpecializations6 () : Unit {
        body (...) {}
        adjoint (...) {} 
        controlled (cs, ...) {} 
        adjoint controlled (cs, ...) {} 
    }

    operation ValidSetOfSpecializations7 () : Unit {
        body (...) {}
        adjoint (...) {} 
        controlled (cs, ...) {} 
        controlled adjoint (cs, ...) {} 
    }

    operation ValidSetOfSpecializations8 () : Unit {
        body (...) {}
        controlled adjoint (cs, ...) {} 
    }

    operation ValidSetOfSpecializations9 () : Unit {
        body (...) {}
        adjoint (...) {} 
    }

    operation ValidSetOfSpecializations10 () : Unit {
        body (...) {}
        controlled (cs, ...) {} 
    }

    operation ValidSetOfSpecializations11 () : Unit
    is Adj + Ctl {
        body (...) {}
        controlled adjoint (cs, ...) {} 
    }

    operation ValidSetOfSpecializations12 () : Unit
    is Adj + Ctl {
        body (...) {}
        adjoint (...) {} 
    }

    operation ValidSetOfSpecializations13 () : Unit
    is Adj + Ctl {
        body (...) {}
        controlled (cs, ...) {} 
    }


    operation InvalidSetOfSpecializations1 () : Unit {
        body (...) {}
        controlled adjoint auto;
        controlled adjoint auto;
    }

    operation InvalidSetOfSpecializations2 () : Unit {
        body (...) {}
        controlled adjoint auto;
        controlled adjoint (cs, ...) {}
    }

    operation InvalidSetOfSpecializations3 () : Unit {
        body (...) {}
        controlled adjoint (cs, ...) {}
        controlled adjoint (cs, ...) {}
    }

    operation InvalidSetOfSpecializations4 () : Unit {
        body (...) {}
        controlled adjoint auto;
        adjoint controlled auto;
    }

    operation InvalidSetOfSpecializations5 () : Unit {
        body (...) {}
        controlled adjoint (cs, ...) {}
        adjoint controlled auto;
    }

    operation InvalidSetOfSpecializations6 () : Unit {
        body (...) {}
        controlled adjoint (cs, ...) {}
        adjoint controlled (cs, ...) {}
    }

    operation InvalidSetOfSpecializations7 () : Unit {
        body (...) {}
        adjoint auto;
        adjoint auto;
    }

    operation InvalidSetOfSpecializations8 () : Unit {
        body (...) {}
        adjoint auto;
        adjoint (...) {}
    }

    operation InvalidSetOfSpecializations9 () : Unit {
        body (...) {}
        adjoint (...) {}
        adjoint (...) {}
    }

    operation InvalidSetOfSpecializations10 () : Unit {
        body (...) {}
        controlled auto;
        controlled auto;
    }

    operation InvalidSetOfSpecializations11 () : Unit {
        body (...) {}
        controlled (cs, ...) {}
        controlled auto;
    }

    operation InvalidSetOfSpecializations12 () : Unit {
        body (...) {}
        controlled (cs, ...) {}
        controlled (cs, ...) {}
    }


    // all paths return a value or fail

    function AllPathsReturnValue1 () : Int {
        return 1; 
    }

    function AllPathsReturnValue2 () : Int {
        if (true) { return 1; }
        else { return 1; }
    }

    function AllPathsReturnValue3 () : Int {
        if (true) { return 1; }
        elif (true) { return 1; }
        else { return 1; }
    }

    function AllPathsReturnValue4 () : Int {
        if (true) { 
            if (true) { return 1; }
            else { return 1; }
        } 
        else { return 1; }
    }

    function AllPathsReturnValue5 () : Int {
        if (true) { return 1; } 
        else {  
            if (true) { return 1; }
            else { return 1; }
        }
    }

    operation AllPathsReturnValue6 () : Int {
        repeat { return 1; }
        until (true) 
        fixup {}
    }

    operation AllPathsReturnValue7 () : Int {
        repeat { 
            if (true) { return 1; }
            else { return 1; }
        }
        until (true) 
        fixup {}
    }

    operation AllPathsReturnValue8 () : Int {
        using q = Qubit() {
            return 1; 
        }
    }

    operation AllPathsReturnValue9 () : Int {
        borrowing q = Qubit() {
            return 1; 
        }
    }

    operation AllPathsReturnValue10 () : Int {
        using q = Qubit() {
            repeat { return 1; }
            until (true);
        }
    }

    operation AllPathsReturnValue11 () : Int {
        borrowing q = Qubit() {
            repeat { return 1; }
            until (true);
        }
    }

    operation AllPathsReturnValue12 (cond : Bool) : Int {
        if (cond) {
            borrowing q = Qubit() {
                repeat { return 1; }
                until (true);
            }
        }
        else {
            using q = Qubit() {
                repeat { return 1; }
                until (true)
                fixup {}
            }        
        }
    }

    operation AllPathsReturnValue13 () : Int {
        within {
            return 1;
        }
        apply {}
    }

    operation AllPathsReturnValue14 () : Int {
        within {}
        apply {
            return 1;
        }
    }


    function AllPathsFail1 () : Int {
        fail ""; 
    }

    function AllPathsFail2 () : Int {
        if (true) { fail ""; }
        else { fail ""; }
    }

    function AllPathsFail3 () : Int {
        if (true) { fail ""; }
        elif (true) { fail ""; }
        else { fail ""; }
    }

    function AllPathsFail4 () : Int {
        if (true) { 
            if (true) { fail ""; }
            else { fail ""; }
        } 
        else { fail ""; }
    }

    function AllPathsFail5 () : Int {
        if (true) { fail ""; } 
        else {  
            if (true) { fail ""; }
            else { fail ""; }
        }
    }

    operation AllPathsFail6 () : Int {
        repeat { fail ""; }
        until (true);
    }

    operation AllPathsFail7 () : Int {
        repeat { 
            if (true) { fail ""; }
            else { fail ""; }
        }
        until (true) 
        fixup {}
    }

    operation AllPathsFail8 () : Int {
        using q = Qubit() {
            fail ""; 
        }
    }

    operation AllPathsFail9 () : Int {
        borrowing q = Qubit() {
            fail ""; 
        }
    }

    operation AllPathsFail10 () : Int {
        using q = Qubit() {
            repeat { fail ""; }
            until (true)
            fixup {}
        }
    }

    operation AllPathsFail11 () : Int {
        borrowing q = Qubit() {
            repeat { fail ""; }
            until (true)
            fixup {}
        }
    }

    operation AllPathsFail12 (cond : Bool) : Int {
        if (cond) {
            borrowing q = Qubit() {
                repeat { fail ""; }
                until (true);
            }
        }
        else {
            using q = Qubit() {
                repeat { fail ""; }
                until (true);
            }        
        }
    }

    operation AllPathsFail13 () : Int {
        within {
            fail "";
        }
        apply {}
    }

    operation AllPathsFail14 () : Int {
        within {}
        apply {
            fail "";
        }
    }


    // not all paths return a value or fail

    function NotAllPathsReturnValue1 () : Int {
        if (true) { return 1; }
    }

    function NotAllPathsReturnValue2 () : Int {
        if (true) {}
        else { return 1; }
    }

    function NotAllPathsReturnValue3 () : Int {
        if (true) { return 1; }
        elif (true) { }
        else { return 1; }
    }

    function NotAllPathsReturnValue4 () : Int {
        if (true) { 
            if (true) { return 1; }
        }       
        else { return 1; }
    }

    function NotAllPathsReturnValue5 () : Int {
        if (true) { 
            if (true) {}
            else { return 1; }
        }       
        else { return 1; }
    }

    function NotAllPathsReturnValue6 () : Int {
        if (true) { return 1; }       
        else { 
            if (true) { return 1; }        
        }
    }

    operation NotAllPathsReturnValue7 () : Int {
        if (true) { return 1; }       
        else { 
            if (true) {}
            else { return 1; }        
        }
    }

    operation NotAllPathsReturnValue8 () : Int {
        repeat {}
        until (true) 
        fixup { return 1; }
    }

    operation NotAllPathsReturnValue9 () : Int {
        repeat {
            if (true) {}
            else { return 1; }
        }
        until (true) 
        fixup {}
    }

    operation NotAllPathsReturnValue10 () : Int {
        repeat {
            if (true) { return 1; }
            elif (true) {}
            else { return 1; }
        }
        until (true) 
        fixup {}
    }

    operation NotAllPathsReturnValue11 () : Int {
        using q = Qubit() {
            repeat {}
            until (true)
            fixup { return 1; }
        }
    }
    
    operation NotAllPathsReturnValue12 () : Int {
        borrowing q = Qubit() {
            repeat {}
            until (true)
            fixup { return 1; }        
        }
    }

    function NotAllPathsReturnValue13 () : Int {
        for i in 1 .. 0 { // empty range
            return 1;     // never executed
        }
    }

    function NotAllPathsReturnValue14 () : Int {
        while (false) {
            return 1; 
        }
    }


    function NotAllPathsReturnOrFail1 () : Int {
        if (true) { fail ""; }
    }

    function NotAllPathsReturnOrFail2 () : Int {
        if (true) {}
        else { fail ""; }
    }

    function NotAllPathsReturnOrFail3 () : Int {
        if (true) { fail ""; }
        elif (true) { }
        else { fail ""; }
    }

    function NotAllPathsReturnOrFail4 () : Int {
        if (true) { 
            if (true) { fail ""; }
        }       
        else { fail ""; }
    }

    function NotAllPathsReturnOrFail5 () : Int {
        if (true) { 
            if (true) {}
            else { fail ""; }
        }       
        else { fail ""; }
    }

    function NotAllPathsReturnOrFail6 () : Int {
        if (true) { fail ""; }       
        else { 
            if (true) { fail ""; }        
        }
    }

    operation NotAllPathsReturnOrFail7 () : Int {
        if (true) { fail ""; }       
        else { 
            if (true) {}
            else { fail ""; }        
        }
    }

    operation NotAllPathsReturnOrFail8 () : Int {
        repeat {}
        until (true) 
        fixup { fail ""; }
    }

    operation NotAllPathsReturnOrFail9 () : Int {
        repeat {
            if (true) {}
            else { fail ""; }
        }
        until (true) 
        fixup {}
    }

    operation NotAllPathsReturnOrFail10 () : Int {
        repeat {
            if (true) { fail ""; }
            elif (true) {}
            else { fail ""; }
        }
        until (true) 
        fixup {}
    }

    operation NotAllPathsReturnOrFail11 () : Int {
        using q = Qubit() {
            repeat {}
            until (true)
            fixup { fail ""; }
        }
    }

    operation NotAllPathsReturnOrFail12 () : Int {
        borrowing q = Qubit() {
            repeat {}
            until (true)
            fixup { fail ""; }        
        }
    }

    function NotAllPathsReturnOrFail13 (range : Range) : Int {
        for i in range {
            fail ""; 
        }
    }

    function NotAllPathsReturnOrFail14 (cond : Bool) : Int {
        while (cond) {
            fail ""; 
        }
    }


    // valid uses of return within using

    operation ReturnFromWithinUsing1 () : Unit {
        using q = Qubit() {
            return ();
        }
    }

    operation ReturnFromWithinUsing2 () : Unit {
        using q = Qubit() {
            if (true) { return (); }  
            else {}
        }
    }

    operation ReturnFromWithinUsing3 () : Unit {
        using q = Qubit() {
            if (true) {}
            elif (true) { return (); }  
            else {}
        }
    }

    operation ReturnFromWithinUsing4 () : Unit {
        using q = Qubit() {
            if (true) {}
            elif (true) {}  
            else { return (); }
        }
    }

    operation ReturnFromWithinUsing5 () : Unit {
        using q = Qubit() {
            if (true) { return (); }
            elif (true) { return (); }  
            else { return (); }
        }
    }

    operation ReturnFromWithinUsing6 () : Unit {
        using q = Qubit() {
            repeat { return (); } 
            until(true) 
            fixup { DoNothing(); }
        }
    }

    operation ReturnFromWithinUsing7 () : Unit {
        using q = Qubit() {
            repeat { 
                DoNothing();
                return (); 
            } 
            until(true) 
            fixup { 
                DoNothing();
            }
        }
    }

    operation ReturnFromWithinUsing8 () : Unit {
        using q = Qubit() {
            for i in 1..10 {
                DoNothing();
                return ();
            }
        }
    }


    // invalid uses of return within using

    operation InvalidReturnFromWithinUsing1 () : Unit {
        using q = Qubit() {
            return ();
            DoNothing(); 
        }
    }

    operation InvalidReturnFromWithinUsing2 () : Unit {
        using q = Qubit() {
            return ();
            fail "unreachable";  
        }
    }

    operation InvalidReturnFromWithinUsing3 () : Unit {
        using q = Qubit() {
            return ();
            if (true) { return (); }  
        }
    }

    operation InvalidReturnFromWithinUsing4 () : Unit {
        using q = Qubit() {
            return ();
            repeat {} 
            until(true) 
            fixup {}
        }
    }

    operation InvalidReturnFromWithinUsing5 () : Unit {
        using q = Qubit() {
            repeat {} 
            until(false) 
            fixup { return (); }
        }
    }

    operation InvalidReturnFromWithinUsing6 () : Unit {
        using q = Qubit() {
            within { return(); }
            apply {}
        }
    }

    operation InvalidReturnFromWithinUsing7 () : Unit {
        using q = Qubit() {
            within {}
            apply { return(); }
        }
    }

    operation InvalidReturnFromWithinUsing8 () : Unit {
        using q = Qubit() {
            return ();
            for i in 1..10 {}
        }
    }

    operation InvalidReturnFromWithinUsing9 () : Unit {
        using q = Qubit() {
            if (true) { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinUsing10 () : Unit {
        using q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { return (); }  
            elif (true) { return (); }  
            else { DoNothing(); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinUsing11 () : Unit {
        using q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { DoNothing(); }  
            else { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinUsing12 () : Unit {
        using q = Qubit() {
            if (true) { return (); }
            elif (true) { return (); }  
            else { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinUsing13 () : Unit {
        using q = Qubit() {
            if (true)
            {
                if (true) { DoNothing(); }
                elif (true) { 
                    return (); 
                    DoNothing();
                }
                else { DoNothing(); }
            }
            elif (true) { DoNothing(); }
            else { DoNothing(); }
        }
    }

    operation InvalidReturnFromWithinUsing14 () : Unit {
        using q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { 
                if (true) {
                    return();
                    DoNothing(); 
                }
                elif (true) { DoNothing(); }
                else { DoNothing(); }            
            }
            else { DoNothing(); }
        }
    }

    operation InvalidReturnFromWithinUsing15 () : Unit {
        using q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { DoNothing(); }
            else {
                if (true) { DoNothing(); }
                elif (true) { DoNothing(); }
                else { 
                    return();
                    DoNothing(); 
                }
            }
        }
    }


    // valid uses of return within borrowing

    operation ReturnFromWithinBorrowing1 () : Unit {
        borrowing q = Qubit() {
            return ();
        }
    }

    operation ReturnFromWithinBorrowing2 () : Unit {
        borrowing q = Qubit() {
            if (true) { return (); }  
            else { DoNothing(); }
        }
    }

    operation ReturnFromWithinBorrowing3 () : Unit {
        borrowing q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { return (); }  
            else { DoNothing(); }
        }
    }

    operation ReturnFromWithinBorrowing4 () : Unit {
        borrowing q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { DoNothing(); }  
            else { return (); }
        }
    }

    operation ReturnFromWithinBorrowing5 () : Unit {
        borrowing q = Qubit() {
            if (true) { return (); }
            elif (true) { return (); }  
            else { return (); }
        }
    }

    operation ReturnFromWithinBorrowing6 () : Unit {
        borrowing q = Qubit() {
            repeat { return (); } 
            until(true) 
            fixup { DoNothing(); }
        }
    }

    operation ReturnFromWithinBorrowing7 () : Unit {
        borrowing q = Qubit() {
            repeat { 
                DoNothing();
                return (); 
            } 
            until(true) 
            fixup { 
                DoNothing();
            }
        }
    }

    operation ReturnFromWithinBorrowing8 () : Unit {
        borrowing q = Qubit() {
            for i in 1..10 {
                DoNothing();
                return ();
            }
        }
    }

    // invalid uses of return within borrowing

    operation InvalidReturnFromWithinBorrowing1 () : Unit {
        borrowing q = Qubit() {
            return ();
            DoNothing(); 
        }
    }

    operation InvalidReturnFromWithinBorrowing2 () : Unit {
        borrowing q = Qubit() {
            return ();
            fail "unreachable";  
        }
    }

    operation InvalidReturnFromWithinBorrowing3 () : Unit {
        borrowing q = Qubit() {
            return ();
            if (true) { return (); }  
        }
    }

    operation InvalidReturnFromWithinBorrowing4 () : Unit {
        borrowing q = Qubit() {
            return ();
            repeat {} 
            until(true) 
            fixup {}
        }
    }

    operation InvalidReturnFromWithinBorrowing5 () : Unit {
        borrowing q = Qubit() {
            repeat { DoNothing(); } 
            until(false) 
            fixup { return (); }
        }
    }

    operation InvalidReturnFromWithinBorrowing6 () : Unit {
        borrowing q = Qubit() {
            repeat {
                return ();
                DoNothing(); 
            } 
            until(true) 
            fixup { DoNothing(); }
        }
    }

    operation InvalidReturnFromWithinBorrowing7 () : Unit {
        borrowing q = Qubit() {
            repeat { DoNothing(); } 
            until(true) 
            fixup { 
                return();
                DoNothing(); 
            }
        }
    }

    operation InvalidReturnFromWithinBorrowing8 () : Unit {
        borrowing q = Qubit() {
            within { return(); }
            apply {}
        }
    }

    operation InvalidReturnFromWithinBorrowing9 () : Unit {
        borrowing q = Qubit() {
            within {}
            apply { return(); }
        }
    }

    operation InvalidReturnFromWithinBorrowing10 () : Unit {
        borrowing q = Qubit() {
            return ();
            for i in 1..10 {}
        }
    }

    operation InvalidReturnFromWithinBorrowing11 () : Unit {
        borrowing q = Qubit() {
            if (true) { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinBorrowing12 () : Unit {
        borrowing q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { return (); }  
            else { DoNothing(); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinBorrowing13 () : Unit {
        borrowing q = Qubit() {
            if (true) { DoNothing(); }
            elif (true) { DoNothing(); }  
            else { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinBorrowing14 () : Unit {
        borrowing q = Qubit() {
            if (true) { return (); }
            elif (true) { return (); }  
            else { return (); }
            DoNothing();
        }
    }

    operation InvalidReturnFromWithinBorrowing15 () : Unit {
        borrowing q = Qubit() {
            if (true) { return(); }
            elif (true) { 
                return (); 
                DoNothing();
            }
            else { return(); }
        }
    }

    operation InvalidReturnFromWithinBorrowing16 () : Unit {
        borrowing q = Qubit() {
            if (true) { 
                return(); 
                DoNothing();
            }
            elif (true) { return (); }
            else { return(); }

        }
    }

    operation InvalidReturnFromWithinBorrowing17 () : Unit {
        borrowing q = Qubit() {
            if (true) { return(); }
            elif (true) { return (); }
            else { 
                return(); 
                DoNothing();
            }
        }
    }


    // valid uses of return within multiple possibly nested qubit scopes

    operation ValidReturnPlacement1 () : Unit {
        using q = Qubit() {
            return ();
        }
        DoNothing();
    }

    operation ValidReturnPlacement2 () : Unit {
        borrowing q = Qubit() {
            return ();
        }
        DoNothing();
    }

    operation ValidReturnPlacement3 () : Unit {
        using q = Qubit() {
            if (true) { return (); }
        }
        DoNothing();
    }

    operation ValidReturnPlacement4 () : Unit {
        borrowing q = Qubit() {
            if (true) { return (); }
        }
        DoNothing();
    }

    operation ValidReturnPlacement5 () : Unit {
        using q = Qubit() { 
            if (true) { return (); }
        }
        borrowing q = Qubit() { 
            DoNothing();
            return (); 
        }
    }

    operation ValidReturnPlacement6 () : Unit {
        using q = Qubit() { 
            if (true) { return (); }
        }
        using q = Qubit() {
            DoNothing();
            return (); 
        }
    }

    operation ValidReturnPlacement7 () : Unit {
        using q = Qubit() {
            DoNothing();
            using c = Qubit() {
                return ();            
            }
        }
    }

    operation ValidReturnPlacement8 () : Unit {
        using q = Qubit() {
            DoNothing();
            borrowing c = Qubit() {
                return ();            
            }
        }
    }

    operation ValidReturnPlacement9 () : Unit {
        borrowing q = Qubit() {
            DoNothing();
            using c = Qubit() {
                return ();            
            }
        }
    }

    operation ValidReturnPlacement10 () : Unit {
        borrowing q = Qubit() {
            DoNothing();
            borrowing c = Qubit() {
                return ();            
            }
        }
    }

    operation ValidReturnPlacement11 () : Unit {
        using q = Qubit() {
            if (true) {
                using c = Qubit() { 
                    return (); 
                }
            }
            else {
                DoNothing();
            }
        }
    }

    operation ValidReturnPlacement12 () : Unit {
        using q = Qubit() {
            if (true) { }
            else {
                using c = Qubit() { 
                    return (); 
                }
            }
        }
    }

    operation ValidReturnPlacement13 () : Unit {
        using q = Qubit() {
            repeat {
                using c = Qubit() { 
                    return (); 
                }
            }
            until (true)
            fixup {
                DoNothing();
            }
        }
    }

    operation ValidReturnPlacement14 () : Unit {
        using q = Qubit() {
            for i in 1 .. 10 {
                using c = Qubit() { 
                    if (i == 1) { return (); }
                }  
            }
        }
    }

    operation ValidReturnPlacement15 () : Unit {
        for i in 1 .. 10 {
            using c = Qubit() { 
                if (i == 1) { return (); }
            }  
            DoNothing();
        }
    }


    // invalid uses of return within multiple possibly nested qubit scopes

    operation InvalidReturnPlacement1 () : Unit {
        using q = Qubit() {
            using c = Qubit() {
                return ();            
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement2 () : Unit {
        using q = Qubit() {
            borrowing c = Qubit() {
                return ();            
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement3 () : Unit {
        borrowing q = Qubit() {
            using c = Qubit() {
                return ();            
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement4 () : Unit {
        borrowing q = Qubit() {
            borrowing c = Qubit() {
                return ();            
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement5 () : Unit {
        using q = Qubit() {
            if (true) {
                using c = Qubit() { 
                    return (); 
                }
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement6 () : Unit {
        using q = Qubit() {
            if (true) {}
            else {
                using c = Qubit() { 
                    return (); 
                }
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement7 () : Unit {
        using q = Qubit() {
            if (true) { }
            else {
                using c = Qubit() { 
                    return (); 
                }
                DoNothing();
            }
        }
    }

    operation InvalidReturnPlacement8 () : Unit {
        using q = Qubit() {
            repeat {}
            until (true)
            fixup {
                using c = Qubit() { 
                    return (); 
                }
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement9 () : Unit {
        using q = Qubit() {
            repeat {}
            until (true)
            fixup {
                using c = Qubit() { 
                    return (); 
                }
                DoNothing();
            }
        }
    }

    operation InvalidReturnPlacement10 () : Unit {
        using q = Qubit() {
            repeat {
                using c = Qubit() { 
                    return (); 
                }            
            }
            until (true)
            fixup {}
            DoNothing();
        }
    }

    operation InvalidReturnPlacement11 () : Unit {
        using q = Qubit() {
            repeat {
                using c = Qubit() { 
                    return (); 
                }            
                DoNothing();
            }
            until (true)
            fixup {}
        }
    }

    operation InvalidReturnPlacement12 () : Unit {
        using q = Qubit() {
            for i in 1 .. 10 {
                using c = Qubit() { 
                    if (i == 1) { return (); }
                }  
            }
            DoNothing();
        }
    }

    operation InvalidReturnPlacement13 () : Unit {
        using q = Qubit() {
            for i in 1 .. 10 {
                using c = Qubit() { 
                    if (i == 1) { return (); }
                }  
                DoNothing();
            }
        }
    }

    operation InvalidReturnPlacement14 () : Unit {
        using q = Qubit() {
            for i in 1 .. 10 {
                using c = Qubit() { 
                    if (i == 1) { return (); }
                    DoNothing();
                }  
            }
        }
    }

    operation InvalidReturnPlacement15 () : Unit {
        using q = Qubit() {
            repeat {}
            until (true)
            fixup {
                using c = Qubit() { 
                    return (); 
                }
            }
        }
    }

    operation InvalidReturnPlacement16 () : Unit {
        using q = Qubit() {
            within {}
            apply { 
                using c = Qubit() { 
                    return (); 
                }
            }
        }
    }

    operation InvalidReturnPlacement17 () : Unit {
        using q = Qubit() {
            within { 
                using c = Qubit() { 
                    return (); 
                }            
            }
            apply { }
        }
    }

    operation InvalidReturnPlacement18 (arg : Bool) : Unit {
        using q = Qubit() {
            within {}
            apply { 
                if (arg) { return (); }
            }
        }
    }

    operation InvalidReturnPlacement19 (arg : Bool) : Unit {
        using q = Qubit() {
            within { 
                if (arg) { return (); }
            }
            apply { }
        }
    }

    operation InvalidReturnPlacement20 () : Unit {
        using q = Qubit() {
            within { 
                within { 
                    return(); 
                    Function();
                }
                apply {}
            }
            apply { DoNothing(); } 
        }
    }


    // valid declaration attributes

    @ Attribute()
    newtype CustomAttribute = Double;
    @ Attribute()
    newtype AttAsUserDefType = CustomAttribute;
    @ Attribute()
    newtype AttArrayAsUserDefType = CustomAttribute[];

    @ Attribute()
    @ AttType1()
    newtype AttType1 = Unit;
    @ Attribute()
    @ AttType2()
    newtype AttType2 = Unit;


    @ IntAttribute(0b111)
    @ StringAttribute("")
    @ BigIntArrayAttribute([0xF0a00101L])
    @ PauliResultAttribute (PauliX, Zero)
    operation ValidAttributes1 () : Unit {}

    @ IntAttribute(0b111)
    @ StringAttribute("")
    @ BigIntArrayAttribute([0xF0a00101L])
    @ PauliResultAttribute (PauliX, Zero)
    function ValidAttributes2 () : Unit {}

    @ IntAttribute(0b111)
    @ StringAttribute("")
    @ BigIntArrayAttribute([0xF0a00101L])
    @ PauliResultAttribute (PauliX, Zero)
    newtype ValidAttributes3 = Unit;

    @ BigIntArrayAttribute(new BigInt[3])
    function ValidAttributes4 () : Unit {}

    @ StringAttribute($"some text")
    function ValidAttributes5 () : Unit {}

    @ CustomAttribute(1.)
    function ValidAttributes6 () : Unit {}

    @ Microsoft.Quantum.Testing.Attributes.CustomAttribute()
    function ValidAttributes7 () : Unit {}

    @ Microsoft.Quantum.Core.IntTupleAttribute(1,1)
    function ValidAttributes8 () : Unit {}

    @ Microsoft.Quantum.Testing.Attributes.IntTupleAttribute(1,1)
    function ValidAttributes9 () : Unit {}

    @ AttType1()
    @ AttType2()
    function ValidAttributes10 () : Unit {}

    @ AttType2()
    @ AttType1()
    @ AttType1()
    function AttributeDuplication1 () : Unit {}

    @ StringAttribute("")
    @ StringAttribute("")
    @ AttType1()
    @ AttType2()
    operation AttributeDuplication2 () : Unit {}

    @ AttType1()
    @ AttType1()
    newtype AttributeDuplication3 = Unit;


    // invalid declaration attributes

    @ StringAttribute($"{1}")
    function InvalidAttributes1 () : Unit {}

    @ IntTupleAttribute(1,1)
    function InvalidAttributes2 () : Unit {}

    @ NonExistent()
    function InvalidAttributes3 () : Unit {}

    @ Undefined.NonExistent()
    function InvalidAttributes4 () : Unit {}

    @ Microsoft.Quantum.Core.NonExistent()
    function InvalidAttributes5 () : Unit {}

    @ Microsoft.Quantum.Testing.Attributes.CustomAttribute(1.)
    function InvalidAttributes6 () : Unit {}

    @ AttAsUserDefType(CustomAttribute(1.))
    function InvalidAttributes7 () : Unit {}

    @ AttArrayAsUserDefType(new CustomAttribute[0])
    function InvalidAttributes8 () : Unit {}

    operation InvalidAttributes9 () : Unit {    
        @ IntAttribute(1)
        body(...) {}
    }

    operation InvalidAttributes10 () : Unit {    
        body(...) {}
        @ IntAttribute(1)
        adjoint(...) {}
    }

    operation InvalidAttributes11 () : Unit {    
        body(...) {}
        @ IntAttribute(1)
        controlled (cs, ...) {}
    }

    operation InvalidAttributes12 () : Unit {    
        @ IntAttribute(1)
        controlled adjoint (cs, ...) {}
        body(...) {}
    }

    @ Attribute() 
    operation InvalidAttributes13 () : Unit {}

    @ Attribute() 
    function InvalidAttributes14 () : Unit {}
}
