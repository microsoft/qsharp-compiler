// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


// Source Code
namespace Microsoft.Quantum.Testing.IntrinsicMapping {

    @EntryPoint()
    operation IntrinsicMappingTest() : Unit {
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

// Environment Code
namespace Microsoft.Quantum.Testing.IntrinsicMapping {
    
    operation Override() : Unit {
        EnvironmentIntrinsic();
    }

    operation EnvironmentIntrinsic() : Unit {
        body intrinsic;
    }
}

