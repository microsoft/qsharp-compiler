// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for functor auto-generation
namespace Microsoft.Quantum.Testing.FunctorGeneration {

    open Microsoft.Quantum.Testing.General;

    // valid and invalid generator directives

    operation AdjointGenDirective1 () : Unit {
        body (...) {}
        adjoint self;
    }

    operation AdjointGenDirective2 () : Unit {
        body (...) {}
        adjoint invert;
    }

    operation AdjointGenDirective3 () : Unit {
        body (...) {}
        adjoint distribute;
    }

    operation AdjointGenDirective4 () : Unit {
        body (...) {}
        adjoint auto;
    }

    operation AdjointGenDirective5 () : Unit {
        body intrinsic;
        adjoint self;
    }

    operation AdjointGenDirective6 () : Unit {
        body intrinsic;
        adjoint invert;
    }

    operation AdjointGenDirective7 () : Unit {
        body intrinsic;
        adjoint distribute;
    }

    operation AdjointGenDirective8 () : Unit {
        body intrinsic;
        adjoint auto;
    }


    operation ControlledGenDirective1 () : Unit {
        body (...) {}
        controlled self;
    }

    operation ControlledGenDirective2 () : Unit {
        body (...) {}
        controlled invert;
    }

    operation ControlledGenDirective3 () : Unit {
        body (...) {}
        controlled distribute;
    }

    operation ControlledGenDirective4 () : Unit {
        body (...) {}
        controlled auto;
    }

    operation ControlledGenDirective5 () : Unit {
        body intrinsic;
        controlled self;
    }

    operation ControlledGenDirective6 () : Unit {
        body intrinsic;
        controlled invert;
    }

    operation ControlledGenDirective7 () : Unit {
        body intrinsic;
        controlled distribute;
    }

    operation ControlledGenDirective8 () : Unit {
        body intrinsic;
        controlled auto;
    }


    operation ControlledAdjointGenDirective1 () : Unit {
        body (...) {}
        controlled adjoint self;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective2 () : Unit {
        body (...) {}
        controlled adjoint invert;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective3 () : Unit {
        body (...) {}
        controlled adjoint distribute;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective4 () : Unit {
        body (...) {}
        controlled adjoint auto;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective5 () : Unit {
        body intrinsic;
        controlled adjoint self;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective6 () : Unit {
        body intrinsic;
        controlled adjoint invert;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective7 () : Unit {
        body intrinsic;
        controlled adjoint distribute;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective8 () : Unit {
        body intrinsic;
        controlled adjoint auto;
        adjoint auto;
        controlled auto;
    }

    operation ControlledAdjointGenDirective9 (q : Qubit) : Unit {
        body (...) { }
        adjoint self;
        controlled adjoint self;
        controlled auto;
    }

    operation ControlledAdjointGenDirective10 (q : Qubit) : Unit {
        body (...) { }
        adjoint (...) { }
        controlled adjoint self; 
        controlled auto;
    }

    operation ControlledAdjointGenDirective11 (q : Qubit) : Unit {
        body (...) { }
        adjoint invert;
        controlled adjoint self; 
        controlled auto;
    }

    operation ControlledAdjointGenDirective12 (q : Qubit) : Unit {
        body (...) { }
        adjoint self;
        controlled adjoint (cs, ...) {}
        controlled auto;
    }

    operation ControlledAdjointGenDirective13 (q : Qubit) : Unit {
        body (...) { }
        adjoint self;
        controlled adjoint invert;
        controlled auto;
    }

    operation ControlledAdjointGenDirective14 (q : Qubit) : Unit {
        body (...) { }
        adjoint self;
        controlled adjoint distribute;
        controlled auto;
    }


    operation Intrinsic1 () : Unit {
        body intrinsic;
        adjoint (...) {}
    }

    operation Intrinsic2 () : Unit {
        body intrinsic;
        controlled (cs, ...) {}
    }

    operation Intrinsic3 () : Unit {
        body intrinsic;
        controlled adjoint (cs, ...) {}
        adjoint auto;
        controlled auto;
    }

    operation Intrinsic4 () : Unit {
        body (...) {}
        adjoint intrinsic;
    }

    operation Intrinsic5 () : Unit {
        body (...) {}
        controlled intrinsic;
    }

    operation Intrinsic6 () : Unit {
        body (...) {}
        controlled adjoint intrinsic;
        adjoint auto;
        controlled auto;
    }


    // operations with any kind of return value cannot have an Adjoint / Controlled implementation (auto-gen or otherwise)

    operation NeedsUnitReturn1 (q : Qubit) : Result {
        body intrinsic;
        adjoint auto;
    }

    operation NeedsUnitReturn2 (q : Qubit) : Result {
        body intrinsic;
        controlled auto;
    }

    operation NeedsUnitReturn3 (q : Qubit) : Result {
        body intrinsic;
        controlled adjoint auto;
    }

    operation NeedsUnitReturn4 (q : Qubit) : Result {
        body intrinsic;
        adjoint self;
    }

    operation NeedsUnitReturn5 (q : Qubit) : Int {
        body (...) { return 1; }
        adjoint (...) { return 1; }
    }

    operation NeedsUnitReturn6 (q : Qubit) : Int {
        body (...) { return 1; }
        controlled (cs, ...) { return 1; }
    }


    operation UnitReturn1 (q : Qubit) : Unit {
        body intrinsic;
        adjoint auto;
    }

    operation UnitReturn2 (q : Qubit) : Unit {
        body intrinsic;
        controlled auto;
    }

    operation UnitReturn3 (q : Qubit) : Unit {
        body intrinsic;
        controlled adjoint auto;
    }

    operation UnitReturn4 (q : Qubit) : Unit {
        body intrinsic;
        adjoint self;
    }

    operation UnitReturn5 (q : Qubit) : Unit {
        body (...) { return (); }
        adjoint (...) { return (); }
    }

    operation UnitReturn6 (q : Qubit) : Unit {
        body (...) { return (); }
        controlled (cs, ...) { return (); }
    }


    // checking that functor support is verified for local variables and partial application as well

    operation VariableNeedsFunctorSupport1 (q : Qubit) : Unit {
        body (...) { 
            let op = Controllable;
            op(q); 
        }
        adjoint auto;
    }

    operation VariableNeedsFunctorSupport2 (q : Qubit) : Unit {
        body (...) { 
            let op = Adjointable;
            op(q); 
        }
        controlled auto;
    }

    operation VariableNeedsFunctorSupport3 (q : Qubit) : Unit {
        body (...) { 
            (Controllable(_))(q);
        }
        adjoint auto;
    }

    operation VariableNeedsFunctorSupport4 (q : Qubit) : Unit {
        body (...) { 
            (Adjointable(_))(q);
        }
        controlled auto;
    }


    operation VariableWithFunctorSupport1 (q : Qubit) : Unit {
        body (...) { 
            let op = Controllable;
            op(q); 
        }
        controlled auto;
    }

    operation VariableWithFunctorSupport2 (q : Qubit) : Unit {
        body (...) { 
            let op = Adjointable;
            op(q); 
        }
        adjoint auto;
    }

    operation VariableWithFunctorSupport3 (q : Qubit) : Unit {
        body (...) { 
            (Controllable(_))(q);
        }
        controlled auto;
    }

    operation VariableWithFunctorSupport4 (q : Qubit) : Unit {
        body (...) { 
            (Adjointable(_))(q);
        }
        adjoint auto;
    }


    // to auto-generate Adjoint / Controlled, all components need to have an Adjoint / Controlled

    operation NeedsFunctorSupport1 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        adjoint auto;
    }

    operation NeedsFunctorSupport2 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        controlled auto;
    }

    operation NeedsFunctorSupport3 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        controlled adjoint auto;
    }

    operation NeedsFunctorSupport4 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        controlled adjoint (cs, ...) {}
    }

    operation NeedsFunctorSupport5 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        adjoint auto;
    }

    operation NeedsFunctorSupport6 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        controlled auto;
    }

    operation NeedsFunctorSupport7 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        controlled adjoint auto;
    }

    operation NeedsFunctorSupport8 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        controlled adjoint (cs, ...) {}
    }

    operation NeedsFunctorSupport9 (q : Qubit) : Unit
    is Ctl + Adj {
        within { }
        apply { Operation(q); }
    }

    operation NeedsFunctorSupport10 (q : Qubit) : Unit {
        body (...) { 
            within { Controllable(q); }
            apply {}
        }
        controlled adjoint auto;
    }

    operation NeedsFunctorSupport11 (q : Qubit) : Unit {
        body (...) { 
            within { }
            apply { Operation(q); }
        }
        controlled adjoint auto;
    }

    operation NeedsFunctorSupport12 (q : Qubit) : Unit {
        body (...) { 
            within { }
            apply { Operation(q); }
        }
        controlled adjoint (cs, ...) {}
    }

    operation NeedsFunctorSupport13 (q : Qubit) : Unit {
        body (...) { 
            within { }
            apply { Controllable(q); }
        }
        adjoint auto;
    }

    operation NeedsFunctorSupport14(q : Qubit) : Unit {
        within { Operation(q); }
        apply { }
    }

    operation NeedsFunctorSupport15(q : Qubit) : Unit {
        within { Controllable(q); }
        apply { }
    }

    operation NeedsFunctorSupport16(q : Qubit) : Unit {
        body (...) {
            within { Operation(q); }
            apply { }        
        }
        adjoint self;
    }

    operation NeedsFunctorSupport17(q : Qubit) : Unit
    is Adj {
        within { Controllable(q); }
        apply { }        
    }


    operation FunctorSupport1 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        adjoint self;
    }

    operation FunctorSupport2 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        adjoint auto;
    }

    operation FunctorSupport3 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        controlled auto;
    }

    operation FunctorSupport4 (q : Qubit) : Unit {
        body (...) { Unitary(q); }
        controlled adjoint auto;
    }

    operation FunctorSupport5 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        adjoint (...) {}
    }

    operation FunctorSupport6 (q : Qubit) : Unit {
        body (...) { Operation(q); }
        controlled (cs, ...) {}
    }

    operation FunctorSupport7 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        adjoint self;
        controlled adjoint auto;
    }

    operation FunctorSupport8 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        controlled adjoint self;
    }

    operation FunctorSupport9 (q : Qubit) : Unit {
        body (...) { Function(); }
        adjoint auto;
    }

    operation FunctorSupport10 (q : Qubit) : Unit {
        body (...) { Function(); }
        controlled auto;
    }

    operation FunctorSupport11 (q : Qubit) : Unit {
        body (...) { Function(); }
        controlled adjoint auto;
    }

    operation FunctorSupport12 (q : Qubit) : Unit
    is Ctl + Adj {
        within { Adjointable(q); }
        apply {}
    }

    operation FunctorSupport13 (q : Qubit) : Unit {
        body (...) { 
            within { Adjointable(q); }
            apply {}
        }
        controlled adjoint (cs, ...) {}
    }

    operation FunctorSupport14 (q : Qubit) : Unit {
        body (...) { 
            within { Adjointable(q); }
            apply {}
        }
        controlled auto;
    }

    operation FunctorSupport15 (q : Qubit) : Unit {
        body (...) { 
            within {}
            apply { Operation(q); }
        }
        adjoint self;
    }

    operation FunctorSupport16 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Adjointable(q); }
        }
        adjoint auto;
    }

    operation FunctorSupport17 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Controllable(q); }
        }
        controlled auto;
    }

    operation FunctorSupport18 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Unitary(q); }
        }
        controlled adjoint auto;
    }

    operation FunctorSupport19 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Operation(q); }
        }
        adjoint (...) {}
    }

    operation FunctorSupport20 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Operation(q); }
        }
        controlled (cs, ...) {}
    }

    operation FunctorSupport21 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Controllable(q); }
        }
        adjoint self;
        controlled adjoint auto;
    }

    operation FunctorSupport22 (q : Qubit) : Unit {
        body (...) {
            within {}
            apply { Controllable(q); }
        }
        controlled adjoint self;
    }

    operation FunctorSupport23 (q : Qubit) : Unit {
        within {
            Adjointable(q);
            Unitary(q);
            Function();
        }
        apply {}
    }


    // adjoint specializations cannot be auto-generated for operations containing rus loops, return-, or set-statements

    operation InvalidAutoInversion1(q : Qubit) : Unit {
        body (...) {
            return ();            
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion2(q : Qubit) : Unit {
        body (...) {
            repeat {}
            until (true)
            fixup {}
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion3(q : Qubit) : Unit {
        body (...) {
            set _ = 1;
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion4(q : Qubit) : Unit {
        body (...) {
            if (true) { return (); }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion5(q : Qubit) : Unit {
        body (...) {
            if (true) {
                repeat {}
                until (true)
                fixup {}
            }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion6(q : Qubit) : Unit {
        body (...) {
            if (true) { set _ = 1; }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion7(q : Qubit) : Unit {
        body (...) {
            for i in 1..10 { return (); }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion8(q : Qubit) : Unit {
        body (...) {
            for i in 1..10 {
                repeat {}
                until (true)
                fixup {}
            }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion9(q : Qubit) : Unit {
        body (...) {
            for i in 1..10 { set _ = 1; }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion10(q : Qubit) : Unit {
        body (...) {
            using c = Qubit() { return (); }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion11(q : Qubit) : Unit {
        body (...) {
            using c = Qubit() {
                repeat {}
                until (true)
                fixup {}
            }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion12(q : Qubit) : Unit {
        body (...) {
            using c = Qubit() { set _ = 1; }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion13(q : Qubit) : Unit {
        body (...) {
            borrowing c = Qubit() { return (); }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion14(q : Qubit) : Unit {
        body (...) {
            borrowing c = Qubit() {
                repeat {}
                until (true)
                fixup {}
            }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion15(q : Qubit) : Unit {
        body (...) {
            borrowing c = Qubit() { set _ = 1; }
        }
        adjoint auto;    
    }

    operation InvalidAutoInversion16(q : Qubit) : Unit {
        within { return (); }
        apply { }
    }

    operation InvalidAutoInversion17(q : Qubit) : Unit {
        within { set _ = 1; }
        apply { }
    }

    operation InvalidAutoInversion18(q : Qubit) : Unit {
        within {
            repeat {}
            until (true);
        }
        apply { }
    }


    operation ValidInversion1(q : Qubit) : Unit {
        body (...) {
            return ();            
        }
        adjoint self;
    }

    operation ValidInversion2(q : Qubit) : Unit {
        body (...) {
            repeat {}
            until (true)
            fixup {}
        }
        adjoint self;    
    }

    operation ValidInversion3(q : Qubit) : Unit {
        body (...) {
            mutable foo = 0;
            set foo = 1;
        }
        adjoint self;    
    }

    operation ValidInversion4(q : Qubit) : Unit {
        body (...) {
            // NOTE: this is ok because, opposed to a return-statement, 
            // there is no way modify control flow in a meaningful way using a fail
            // (the computation fails either way albeit potentially a bit earlier rather than later)
            fail "not yet implemented";  
        }
        adjoint auto;
    }

    operation ValidInversion5(q : Qubit) : Unit {
        within {}
        apply {
            return ();
        }
    }

    operation ValidInversion6(q : Qubit) : Unit {
        within {}
        apply {
            set _ = 1;
        }
    }

    operation ValidInversion7(q : Qubit) : Unit {
        within {}
        apply {
            repeat {}
            until (true);
        }
    }

    operation ValidInversion8(q : Qubit) : Unit {
        within { fail ""; }
        apply {}
    }


    // adjoint specializations cannot be auto-generated for operation calls outside expression statements

    operation WithInvalidQuantumDependency1 (q : Qubit) : Unit {
        body (...) {
            let _ = Adjointable(q);
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency2 (q : Qubit) : Unit {
        body (...) {
            mutable _ = Adjointable(q);
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency3 (q : Qubit) : Unit {
        body (...) {
            fail $"{Adjointable(q)}";
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency4 (q : Qubit) : Unit {
        body (...) {
            if (CoinFlip()) {}
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency5 (q : Qubit) : Unit {
        body (...) {
            for i in CoinFlip() ? new Int[0] | new Int[1] {}
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency6 (q : Qubit) : Unit {
        body (...) {
            using qs = Qubit[CoinFlip() ? 1 | 0] {}
        }
        adjoint auto;
    }

    operation WithInvalidQuantumDependency7 (q : Qubit) : Unit {
        body (...) {
            borrowing qs = Qubit[CoinFlip() ? 1 | 0] {}
        }
        adjoint auto;
    }


    operation WithoutInvalidQuantumDependency1 (q : Qubit) : Unit {
        body (...) {
            let _ = Operation(_);
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency2 (q : Qubit) : Unit {
        body (...) {
            mutable _ = Operation(_);
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency3 (q : Qubit) : Unit {
        body (...) {
            fail "";
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency4 (q : Qubit) : Unit {
        body (...) {
            if (true) { Adjointable(q); }
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency5 (q : Qubit) : Unit {
        body (...) {
            for i in 1..5 { Adjointable(q); }
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency6 (q : Qubit) : Unit {
        body (...) {
            using c = Qubit() { Adjointable(q); }
        }
        adjoint auto;
    }

    operation WithoutInvalidQuantumDependency7 (q : Qubit) : Unit {
        body (...) {
            borrowing c = Qubit() { Adjointable(q); }
        }
        adjoint auto;
    }


    // auto-generation for controlled on the other hand only requires the necessary functor support

    operation InvalidControlled1 (q : Qubit) : Unit {
        body (...) {
            if (CoinFlip()) {}
        }
        controlled auto;
    }

    operation InvalidControlled2 (q : Qubit) : Unit {
        body (...) {
            for i in CoinFlip() ? new Int[0] | new Int[1] {}
        }
        controlled auto;
    }

    operation InvalidControlled3 (q : Qubit) : Unit {
        body (...) {
            using cs = Qubit[CoinFlip() ? 1 | 0] {}
        }
        controlled auto;
    }

    operation InvalidControlled4 (q : Qubit) : Unit {
        body (...) {
            borrowing cs = Qubit[CoinFlip() ? 1 | 0] {}
        }
        controlled auto;
    }


    operation ValidControlled1 (q : Qubit) : Unit {
        body (...) {
            let _ = Controllable (q);        
        }
        controlled auto;
    }

    operation ValidControlled2 (q : Qubit) : Unit {
        body (...) {
            mutable _ = Controllable (q);        
        }
        controlled auto;
    }

    operation ValidControlled3 (q : Qubit) : Unit {
        body (...) {
            set _ = Controllable (q);        
        }
        controlled auto;
    }

    operation ValidControlled4 (q : Qubit) : Unit {
        body (...) {
            return Controllable(q);        
        }
        controlled auto;
    }

    operation ValidControlled5 (q : Qubit) : Unit {
        body (...) {
            fail $"{Controllable (q)}";        
        }
        controlled auto;
    }

    operation ValidControlled6 (q : Qubit) : Unit {
        body (...) {
            if (true) { Controllable(q); }        
        }
        controlled auto;
    }

    operation ValidControlled7 (q : Qubit) : Unit {
        body (...) {
            for i in 1..5 { Controllable(q); }
        }
        controlled auto;
    }

    operation ValidControlled8 (q : Qubit) : Unit {
        body (...) {
            using c = Qubit() { Controllable(q); }
        }
        controlled auto;
    }

    operation ValidControlled9 (q : Qubit) : Unit {
        body (...) {
            borrowing c = Qubit() { Controllable(q); }
        }
        controlled auto;
    }


    // verifying the necessary functor support for controlled adjoint auto generation

    operation InvalidControlledAdjointGeneration1 (q : Qubit) : Unit {
        body (...) { }
        adjoint (...) { }
        controlled (cs, ...) { Controllable(q); }
        controlled adjoint invert;
    }

    operation InvalidControlledAdjointGeneration2 (q : Qubit) : Unit {
        body (...) { }
        adjoint (...) { Adjointable(q); }
        controlled (cs, ...) { }
        controlled adjoint distribute;
    }

    operation InvalidControlledAdjointGeneration3 (q : Qubit) : Unit {
        body (...) { }
        adjoint (...) { Adjointable(q); }
        controlled (cs, ...) { }
        controlled adjoint auto;
    }

    operation InvalidControlledAdjointGeneration4 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        adjoint auto;
        controlled (cs, ...) { }
        controlled adjoint distribute;
    }

    operation InvalidControlledAdjointGeneration5 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        adjoint self;
        controlled (cs, ...) { }
        controlled adjoint distribute;
    }

    operation InvalidControlledAdjointGeneration6 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        adjoint invert;
        controlled (cs, ...) { }
        controlled adjoint distribute;
    }

    operation InvalidControlledAdjointGeneration7 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        adjoint (...) { }
        controlled auto;
        controlled adjoint invert;
    }
    

    operation ValidControlledAdjointGeneration1 (q : Qubit) : Unit {
        body (...) { Operation (q); }
        adjoint (...) { Controllable(q); }
        controlled (cs, ...) { Adjointable(q); }
        controlled adjoint auto;
    }

    operation ValidControlledAdjointGeneration2 (q : Qubit) : Unit {
        body (...) { Operation (q); }
        adjoint (...) { Controllable(q); }
        controlled (cs, ...) { Adjointable(q); }
        controlled adjoint invert;
    }

    operation ValidControlledAdjointGeneration3 (q : Qubit) : Unit {
        body (...) { Operation (q); }
        adjoint (...) { Controllable(q); }
        controlled (cs, ...) { Adjointable(q); }
        controlled adjoint distribute;
    }

    operation ValidControlledAdjointGeneration4 (q : Qubit) : Unit {
        body (...) { Adjointable(q); }
        adjoint auto;
        controlled (cs, ...) { Adjointable(q); }
        controlled adjoint auto;
    }

    operation ValidControlledAdjointGeneration5 (q : Qubit) : Unit {
        body (...) { Controllable(q); }
        adjoint (...) { Controllable(q); }
        controlled auto;
        controlled adjoint auto;
    }


    // auto generation of recursive operations

    operation InvalidMutalRecursion1a (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion1b(q); }
    }

    operation InvalidMutalRecursion1b (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion1a(q); }
        adjoint auto;
    }

    operation InvalidMutalRecursion2a (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion2b(q); }
    }

    operation InvalidMutalRecursion2b (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion2a(q); }
        controlled auto;
    }

    operation InvalidMutalRecursion3a (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion3b(q); }
    }

    operation InvalidMutalRecursion3b (q : Qubit) : Unit {
        body (...) { InvalidMutalRecursion3a(q); }
        controlled adjoint auto;
    }


    operation Recursion1 (q : Qubit) : Unit {
        body (...) { Recursion1(q); }
        adjoint auto;
    }

    operation Recursion2 (q : Qubit) : Unit {
        body (...) { Recursion2(q); }
        controlled auto;
    }

    operation Recursion3 (q : Qubit) : Unit {
        body (...) { Recursion3(q); }
        controlled adjoint auto;
    }

    operation MutalRecursion1a (q : Qubit) : Unit {
        body (...) { MutalRecursion1b(q); }
        adjoint auto;
    }

    operation MutalRecursion1b (q : Qubit) : Unit {
        body (...) { MutalRecursion1a(q); }
        adjoint auto;
    }

    operation MutalRecursion2a (q : Qubit) : Unit {
        body (...) { MutalRecursion2b(q); }
        controlled auto;
    }

    operation MutalRecursion2b (q : Qubit) : Unit {
        body (...) { MutalRecursion2a(q); }
        controlled auto;
    }

    operation MutalRecursion3a (q : Qubit) : Unit {
        body (...) { MutalRecursion3b(q); }
        controlled adjoint auto;
    }

    operation MutalRecursion3b (q : Qubit) : Unit {
        body (...) { MutalRecursion3a(q); }
        controlled adjoint auto;
    }


    // utils for testing operation characteristics expressions

    operation NoAffiliation () : Unit {}
    operation AdjAffiliation () : Unit is Adj {}
    operation CtlAffiliation () : Unit is Ctl {}
    operation CtlAdjAffiliation () : Unit is Ctl + Adj {}
    operation AdjCtlAffiliation () : Unit is Adj + Ctl {}

    operation AdjCtlIntersection () : Unit is Adj * Ctl {}
    operation CtlAdjIntersection () : Unit is Ctl * Adj {}
    operation CtlViaIntersection () : Unit is Ctl * (Adj + Ctl) {}
    operation AdjViaIntersection () : Unit is Adj * (Ctl + Adj) {}
    operation EmptyIntersection1 () : Unit is (Ctl * Adj) * (Adj + Ctl) {}
    operation EmptyIntersection2 () : Unit is (Ctl + Adj) * (Ctl * Adj) {}


    operation AutoAdjSpec () : Unit {
        adjoint auto; 
    }

    operation InvertAdjSpec () : Unit {
        adjoint invert; 
    }

    operation SelfAdjSpec () : Unit {
        adjoint self; 
    }

    operation AutoCtlSpec () : Unit {
        controlled auto;
    } 

    operation DistrCtlSpec () : Unit {
        controlled distribute; 
    }

    operation CtlAffAutoAdjSpec () : Unit is Ctl {
        adjoint auto; 
    }

    operation CtlAffInvertAdjSpec () : Unit is Ctl {
        adjoint invert; 
    }

    operation CtlAffSelfAdjSpec () : Unit is Ctl {
        adjoint self; 
    }

    operation AdjAffAutoCtlSpec () : Unit is Adj {
        controlled auto;
    } 

    operation AdjAffDistrCtlSpec () : Unit is Adj {
        controlled distribute; 
    }


    // operation characteristics expressions

    operation OperationCharacteristics1 (qs : Qubit[]) : Unit {
        NoAffiliation(); 
        AdjAffiliation(); 
        CtlAffiliation();
        CtlAdjAffiliation();
        AdjCtlAffiliation();
        CtlViaIntersection();
        AdjViaIntersection();
        EmptyIntersection1();
        EmptyIntersection2();
        Adjoint AdjAffiliation();
        Adjoint CtlAdjAffiliation();
        Adjoint AdjCtlAffiliation();
        Adjoint AdjViaIntersection();
        Controlled CtlAffiliation(qs, ());
        Controlled CtlAdjAffiliation(qs, ()); 
        Controlled AdjCtlAffiliation(qs, ()); 
        Controlled CtlViaIntersection(qs, ()); 
    }

    operation OperationCharacteristics2 (qs : Qubit[]) : Unit {
        Adjoint NoAffiliation(); 
    }

    operation OperationCharacteristics3 (qs : Qubit[]) : Unit {
        Controlled NoAffiliation((), qs); 
    }

    operation OperationCharacteristics4 (qs : Qubit[]) : Unit {
        Controlled AdjAffiliation((), qs);        
    }

    operation OperationCharacteristics5 (qs : Qubit[]) : Unit {
        Adjoint CtlAffiliation();        
    }

    operation OperationCharacteristics6 (qs : Qubit[]) : Unit {
        Adjoint AdjCtlIntersection();        
    }

    operation OperationCharacteristics7 (qs : Qubit[]) : Unit {
        Adjoint CtlAdjIntersection();        
    }

    operation OperationCharacteristics8 (qs : Qubit[]) : Unit {
        Adjoint CtlViaIntersection();        
    }

    operation OperationCharacteristics9 (qs : Qubit[]) : Unit {
        Controlled AdjCtlIntersection(qs, ());        
    }

    operation OperationCharacteristics10 (qs : Qubit[]) : Unit {
        Controlled CtlAdjIntersection(qs, ());        
    }

    operation OperationCharacteristics11 (qs : Qubit[]) : Unit {
        Controlled AdjViaIntersection(qs, ());        
    }

    operation OperationCharacteristics12 (qs : Qubit[]) : Unit {
        Adjoint EmptyIntersection1();        
    }

    operation OperationCharacteristics13 (qs : Qubit[]) : Unit {
        Adjoint EmptyIntersection2();        
    }

    operation OperationCharacteristics14 (qs : Qubit[]) : Unit {
        Controlled EmptyIntersection1(qs, ());        
    }

    operation OperationCharacteristics15 (qs : Qubit[]) : Unit {
        Controlled EmptyIntersection2(qs, ());        
    }

    operation OperationCharacteristics16 (qs : Qubit[]) : Unit {
        CtlAffAutoAdjSpec(); 
        CtlAffInvertAdjSpec(); 
        CtlAffSelfAdjSpec();
        Adjoint CtlAffAutoAdjSpec();
        Adjoint CtlAffInvertAdjSpec();
        Adjoint CtlAffSelfAdjSpec();
        Controlled CtlAffAutoAdjSpec(qs, ()); 
        Controlled CtlAffInvertAdjSpec(qs, ()); 
        Controlled CtlAffSelfAdjSpec(qs, ()); 
    }

    operation OperationCharacteristics17 (qs : Qubit[]) : Unit {
        AdjAffAutoCtlSpec(); 
        AdjAffDistrCtlSpec();
        Adjoint AdjAffAutoCtlSpec();
        Adjoint AdjAffDistrCtlSpec();
        Controlled AdjAffAutoCtlSpec(qs, ()); 
        Controlled AdjAffDistrCtlSpec(qs, ()); 
    }

    operation OperationCharacteristics18 (qs : Qubit[]) : Unit {
        AutoCtlSpec(); 
        DistrCtlSpec();
        AutoAdjSpec(); 
        InvertAdjSpec(); 
        SelfAdjSpec();
        Controlled AutoCtlSpec(qs, ()); 
        Controlled DistrCtlSpec(qs, ()); 
        Adjoint AutoAdjSpec();
        Adjoint InvertAdjSpec();
        Adjoint SelfAdjSpec();
    }

    operation OperationCharacteristics19 (qs : Qubit[]) : Unit {
        Adjoint AutoCtlSpec();
    }

    operation OperationCharacteristics20 (qs : Qubit[]) : Unit {
        Adjoint DistrCtlSpec();
    }

    operation OperationCharacteristics21 (qs : Qubit[]) : Unit {
        Controlled AutoAdjSpec(qs, ()); 
    }

    operation OperationCharacteristics22 (qs : Qubit[]) : Unit {
        Controlled InvertAdjSpec(qs, ()); 
    }

    operation OperationCharacteristics23 (qs : Qubit[]) : Unit {
        Controlled SelfAdjSpec(qs, ()); 
    }

}
