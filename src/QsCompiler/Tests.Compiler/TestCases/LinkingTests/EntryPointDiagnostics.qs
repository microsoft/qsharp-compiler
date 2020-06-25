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

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint1() : Result {
        return Zero;
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint2() : Result[] {
        return [Zero];    
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint3() : (Result, Result[]) {
        return (Zero, [Zero]);    
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint4() : ((Result, Result), Result[]) {
        return ((Zero, Zero), [Zero]);
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint5() : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint6() : (Int, Result) {
        return (0, Zero);
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint7() : String[] {
        return [""];
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation EntryPoint8() : ((Result[], (Result, Int)[]), Result) {
        return (([Zero], [(Zero, 0)]), Zero);
    }
}

