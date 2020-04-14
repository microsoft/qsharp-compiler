// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The entry points in this file are all recognized as such. 
// Correspondingly, checking the generated diagnostics requires to compile them separately. 

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPoint42(argName : Int, arg_name : Int) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPoint43(argName : Int, ArgName : Int) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPoint43(argName : Int, Arg_Name : Int) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPoint44(simulator : Int) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation InvalidEntryPoint45(s : Int) : Unit {}
}
