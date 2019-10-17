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

}
