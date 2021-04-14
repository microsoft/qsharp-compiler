// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Diagnostics;

    @EntryPoint()
    function DumpMachineToFileTest(filePath : String) : Unit {
        DumpMachine(filePath);
    }

    @EntryPoint()
    function DumpMachineTest() : Unit {
        DumpMachine();
    }

    @EntryPoint()
    operation DumpRegisterTest() : Unit {
        use q2 = Qubit[2];
        DumpRegister((), q2);
    }

    @EntryPoint()
    operation DumpRegisterToFileTest(filePath : String) : Unit {
        use q2 = Qubit[2];
        DumpRegister(filePath, q2);
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T>(arg : 'T) : Unit {
        body intrinsic;
    }

    function DumpRegister<'T>(arg : 'T, qs : Qubit[]) : Unit {
        body intrinsic;
    }
}
