// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.EntryPoints {

    // testing argument and return type restrictions for entry points
    // -> the entry point attributes below are attached to declarations that cannot be used as entry points and will be ignored 

    @ EntryPoint()
    function InvalidEntryPoint1 (arg : Qubit) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint2 (arg : (Int, Qubit)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint3 (arg : Qubit[]) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint4 () : Qubit {
        return Default<Qubit>();
    }

    @ EntryPoint()
    function InvalidEntryPoint5 () : ((Int, Qubit), Int) {
        return Default<((Int, Qubit), Int)>();
    }

    @ EntryPoint()
    function InvalidEntryPoint6 () : (Int, Qubit)[] {
        return Default<(Int, Qubit)[]>();
    }


    @ EntryPoint()
    function InvalidEntryPoint7 (arg : (Unit -> Unit)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint8 (arg : ((Unit -> Unit), Int)) : Unit {}

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
    function InvalidEntryPoint12 () : (BigInt, (Unit -> Unit)) {
        return Default<(BigInt, (Unit -> Unit))>();
    }


    @ EntryPoint()
    function InvalidEntryPoint13 () : (Qubit -> Unit) {
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
    function InvalidEntryPoint18 (arg : ((Unit => Unit), Int)) : Unit {}

    @ EntryPoint()
    function InvalidEntryPoint19 (arg : ((Unit => Unit is Ctl)[], Int)) : Unit {}

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
    function InvalidEntryPoint23 () : (Qubit => Unit is Adj) {
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
    function InvalidEntryPoint26 () : (Unit => (Int, Qubit) is Ctl + Adj) {
        return Default<(Unit => (Int, Qubit) is Ctl + Adj)>();
    }

}
