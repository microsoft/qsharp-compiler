// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace SubOps {
    operation SubOp1() : Unit { }
    operation SubOp2() : Unit { }
    operation SubOp3() : Unit { }
}

// =================================

namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    @EntryPoint()
    operation ClassicalControlTest1() : Unit {
        Foo();
    }

    operation Foo() : Unit {
        let r = One;
        if (r == One) {
            SubOp1();
            SubOp2();
            SubOp3();
        }
    }

}

// =================================

namespace Microsoft.Quantum.Testing.ClassicalControl {
    open SubOps;

    @EntryPoint()
    operation ClassicalControlTest2() : Unit {
        Foo();
    }

    operation Foo() : Unit {
        let r = One;
        if (r == One) {
            SubOp1();
        }
    }

}