// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The entry point attributes in this file are attached to declarations
// that cannot be used as entry points and will be thus be ignored.

namespace Microsoft.Quantum.Testing.EntryPoints {

    // tests related to entry point placement verification

    @ EntryPoint()
    operation EntryPointInLibrary() : Unit { }

    @ EntryPoint()
    newtype InvalidEntryPointPlacement1 = Int;

    @ EntryPoint()
    function InvalidEntryPointPlacement2<'T> (a : 'T) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPointPlacement3<'T> (a : 'T) : Unit {}

    operation InvalidEntryPointPlacement4 () : Unit {

        @ EntryPoint()
        body(...) {}    
    }

    operation InvalidEntryPointPlacement5 () : Unit {
        body (...) {}

        @ EntryPoint()
        adjoint (...) {}   
    }

    operation InvalidEntryPointPlacement6 () : Unit {
        body (...) {}

        @ EntryPoint()
        controlled (cs, ...) {}
    }

    operation InvalidEntryPointPlacement7 () : Unit {
        body (...) {}

        @ EntryPoint()
        controlled adjoint (cs, ...) {}   
    }


    // testing argument and return type restrictions for entry points

    @ EntryPoint()
    function InvalidEntryPoint1 (arg : Qubit) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint2 (arg : (Int, Qubit)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint3 (arg : Qubit[]) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint4 () : Qubit {
        return Default<Qubit>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint5 () : ((Int, Qubit), Int) {
        return Default<((Int, Qubit), Int)>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint6 () : (Int, Qubit)[] {
        return Default<(Int, Qubit)[]>();
    }


    @ EntryPoint()
    operation InvalidEntryPoint7 (arg : (Unit -> Unit)) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint8 (arg : ((Unit -> Unit), Int)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint9 (arg : ((Unit -> Unit)[], Int)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint10 () : (Unit -> Unit) {
        return Default<(Unit -> Unit)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint11 () : (Unit -> Unit)[] {
        return Default<(Unit -> Unit)[]>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint12 () : (BigInt, (Unit -> Unit)) {
        return Default<(BigInt, (Unit -> Unit))>();
    }


    @ EntryPoint()
    operation InvalidEntryPoint13 () : (Qubit -> Unit) {
        return Default<(Qubit -> Unit)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint14 () : (Unit -> Qubit) {
        return Default<(Unit -> Qubit)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint15 () : (Qubit[] -> Unit) {
        return Default<(Qubit[] -> Unit)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint16 () : (Unit -> (Int, Qubit)) {
        return Default<(Unit -> (Int, Qubit))>();
    }


    @ EntryPoint()
    function InvalidEntryPoint17 (arg : (Unit => Unit is Adj)) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint18 (arg : ((Unit => Unit), Int)) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint19 (arg : ((Unit => Unit is Ctl)[], Int)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint20 () : (Unit => Unit is Ctl + Adj) {
        return Default<(Unit => Unit is Ctl + Adj)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint21 () : (Unit => Unit)[] {
        return Default<(Unit => Unit)[]>();
    }

    @ EntryPoint()
    function InvalidEntryPoint22 () : (BigInt, (Unit => Unit)) {
        return Default<(BigInt, (Unit => Unit))>();
    }


    @ EntryPoint()
    operation InvalidEntryPoint23 () : (Qubit => Unit is Adj) {
        return Default<(Qubit => Unit is Adj)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint24 () : (Unit => Qubit is Ctl) {
        return Default<(Unit => Qubit is Ctl)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint25 () : (Qubit[] => Unit) {
        return Default<(Qubit[] => Unit)>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint26 () : (Unit => (Int, Qubit) is Ctl + Adj) {
        return Default<(Unit => (Int, Qubit) is Ctl + Adj)>();
    }


    // arguments and return values of user defined type

    newtype Complex = (Re : Double, Im : Double);

    @ EntryPoint()
    operation InvalidEntryPoint27 (arg : Complex) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint28 (arg : (Int, Complex)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint29 (arg : Complex[]) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint30 () : Complex {
        return Default<Complex>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint31 () : ((Int, Complex), Int) {
        return Default<((Int, Complex), Int)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint32 () : (Int, Complex)[] {
        return Default<(Int, Complex)[]>();
    }


    @ EntryPoint()
    operation InvalidEntryPoint33 () : (Complex => Unit is Adj) {
        return Default<(Complex => Unit is Adj)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint34 () : (Unit => Complex is Ctl) {
        return Default<(Unit => Complex is Ctl)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint35 () : (Complex[] => Unit) {
        return Default<(Complex[] => Unit)>();
    }

    @ EntryPoint()
    operation InvalidEntryPoint36 () : (Unit => (Int, Complex) is Ctl + Adj) {
        return Default<(Unit => (Int, Complex) is Ctl + Adj)>();
    }


    // no inner tuples in entry point arguments
    
    @ EntryPoint()
    operation InvalidEntryPoint37(arg : (Int, BigInt[])) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint38(arg1 : (Pauli, Result)[], arg2 : Double) : Unit {}


    // array item type validation in entry point arguments

    @ EntryPoint()
    operation InvalidEntryPoint39(arg : Int[][]) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint40(arg : (Int[])[]) : Unit {}

    @ EntryPoint()
    operation InvalidEntryPoint41(a : Int[], (b : Int[][], c : Double)) : Unit {}
}
