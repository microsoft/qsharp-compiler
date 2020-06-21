// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.Linking {

    newtype BigEndian = Qubit[];
    operation Foo () : Unit {}
    function Bar() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    internal newtype BigEndian = Qubit[];
    internal operation Foo () : Unit {}
    internal function Bar() : Unit {}
}

