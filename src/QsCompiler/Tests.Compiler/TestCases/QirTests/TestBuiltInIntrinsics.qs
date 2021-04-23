// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    newtype Options = (
        VerboseMessage: (String -> Unit)
    );

    function Ignore<'T> (arg : 'T) : Unit {}

    function DefaultOptions() : Options {
        return Options(
            Ignore<String>
        );
    }

    @EntryPoint()
    operation TestBuiltInIntrinsics() : Unit {
        let options = DefaultOptions()
            w/ VerboseMessage <- Message;
    }
}

namespace Microsoft.Quantum.Intrinsic {

    function Message<'T> (arg : 'T) : Unit {
        body intrinsic;
    }
}
