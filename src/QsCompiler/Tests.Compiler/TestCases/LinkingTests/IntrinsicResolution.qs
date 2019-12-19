// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


// Source Code - Test 1
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest1() : Unit {
        LocalIntrinsic();
        Override();
    }

    operation LocalIntrinsic() : Unit {
        body intrinsic;
    }

    operation Override() : Unit {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 1
namespace Microsoft.Quantum.Testing.IntrinsicResolution {
    
    operation Override() : Unit {
        EnvironmentIntrinsic();
    }

    operation EnvironmentIntrinsic() : Unit {
        body intrinsic;
    }
}

// =================================

// Source Code - Test 2
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest2() : Unit {
        let _ = Override();
    }

    newtype TestType = Unit;

    operation Override() : TestType {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 2
namespace Microsoft.Quantum.Testing.IntrinsicResolution {
    
    newtype TestType = Unit;

    operation Override() : TestType {
        return TestType();
    }
}

// =================================

// Source Code - Test 3
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest3() : Unit {
        Override();
    }

    operation Override() : Unit {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 3
namespace Microsoft.Quantum.Testing.IntrinsicResolution {
    
    newtype TestType = Unit;

    // this should cause an error
    operation Override() : TestType {
        return TestType();
    }
}

// =================================

// Source Code - Test 4
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest4() : Unit {
        Override(TestType());
    }

    newtype TestType = Unit;

    operation Override(x : TestType) : Unit {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 4
namespace Microsoft.Quantum.Testing.IntrinsicResolution {
    
    newtype TestType = Unit;

    operation Override(x : TestType) : Unit {
        return ();
    }
}

// =================================

// Source Code - Test 5
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest5() : Unit {
        Override();
    }

    operation Override() : Unit is Adj {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 5
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    operation Override() : Unit is Adj {
        body (...) {
            return ();
        }

        adjoint (...) {
            return ();
        }
    }
}

// =================================

// Source Code - Test 6
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    @EntryPoint()
    operation IntrinsicResolutionTest5() : Unit {
        Override();
    }

    operation Override() : Unit is Adj {
        body intrinsic;
    }
}

// =================================

// Environment Code - Test 6
namespace Microsoft.Quantum.Testing.IntrinsicResolution {

    // this should cause an error
    operation Override() : Unit is Ctl {
        return ();
    }
}
