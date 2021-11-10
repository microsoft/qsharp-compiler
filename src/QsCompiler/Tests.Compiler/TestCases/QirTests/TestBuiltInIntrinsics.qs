// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Llvm;

    newtype Options = (
        SimpleMessage: (String -> Unit),
        DumpToFile: (String -> Unit),
        DumpToConsole: (Unit -> Unit)
    );

    function Ignore<'T> (arg : 'T) : Unit {}

    function DefaultOptions() : Options {
        return Options(
            Ignore,
            Ignore,
            Ignore
        );
    }

    @EntryPoint()
    operation TestBuiltInIntrinsics() : Int {
        let options = DefaultOptions()
            w/ SimpleMessage <- Message
            w/ DumpToFile <- DumpMachine
            w/ DumpToConsole <- DumpMachine;

        options::SimpleMessage("Hello");
        options::DumpToFile("pathToFile");
        options::DumpToConsole();
        return ReadCycleCounter();
    }
}

namespace Microsoft.Quantum.Intrinsic {

    function Message (arg : String) : Unit {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Diagnostics {

    function DumpMachine<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Llvm {

    operation ReadCycleCounter() : Int {
        body intrinsic;
    }
}
