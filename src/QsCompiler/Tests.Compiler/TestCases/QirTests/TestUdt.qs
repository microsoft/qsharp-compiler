// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    internal newtype Foo = Unit;
    internal newtype Bar = Int;
    newtype TestType = ((Pauli, I : Int), D : Double);

    @EntryPoint()
    operation Main() : Unit {
        let _ = TestType((PauliI, 0), 0.);
        let _ = [Foo()];
        let _ = [Bar(1)];
    }
}
