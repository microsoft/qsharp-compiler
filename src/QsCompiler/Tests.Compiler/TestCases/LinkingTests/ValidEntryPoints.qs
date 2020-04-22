// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The entry points in this file are all recognized as such. 
// Correspondingly, checking the generated diagnostics requires to compile them separately. 

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    function ValidEntryPoint1() : Unit { }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint2() : Unit { }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint3() : Unit {
        body (...) {}
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint4(arg : String) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint5(arg : String[]) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint6(a : Int, b: Double[]) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint7(arg1 : (Int)[], arg2 : Double) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint8((b1 : Bool, b2 : Bool), r : Range) : Unit {}
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint9() : Result {
        return Default<Result>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint10() : Result[] {
        return Default<Result[]>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint11() : (Result, Result[]) {
        return Default<(Result, Result[])>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint12() : (Int, BigInt[]) {
        return Default<(Int, BigInt[])>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint13() : (Double, (Pauli, Int)[]) {
        return Default<(Double, (Pauli, Int)[])>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint14() : ((Bool, String)[][], (Unit, Range)) {
        return Default<((Bool, String)[][], (Unit, Range))>();
    }
}

// =================================

namespace Microsoft.Quantum.Testing.EntryPoints {

    @ EntryPoint()
    operation ValidEntryPoint15(a : Int[], (b : Double[], c : String[])) : Unit { }
}
