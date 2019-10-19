// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The entry points in this file are all recognized as such. 
// Correspondingly, checking the generated diagnostics requires to compile them separately. 

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec1() : Unit
    is Adj { }

}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec2() : Unit
    is Ctl { }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec3() : Unit
    is Ctl + Adj { }

}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec4() : Unit {
        body (...) {}
        adjoint self;
    }

}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec5() : Unit {
        body (...) {}
        controlled auto;
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPointSpec6() : Unit {
        body (...) {}
        controlled adjoint (cs, ...) {}
    }
}
