// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.Linking {

    newtype BigEndian = Qubit[];
    operation Foo () : Unit {}
    function Bar() : Unit is Adj + Ctl {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    internal newtype BigEndian = Qubit[];
    internal operation Foo () : Unit is Adj + Ctl {}
    internal function Bar() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    function BigEndian() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    operation BigEndian() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    internal operation Foo () : Unit {}
    internal function Bar() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.Linking {

    operation BigEndian() : Int {
        return 1;
    }
}

// =================================

namespace Microsoft.Quantum.Testing.Linking.Core {

    newtype BigEndian = Qubit[];
    operation Foo () : Unit {}
    function Bar() : Unit is Adj + Ctl {}
}

