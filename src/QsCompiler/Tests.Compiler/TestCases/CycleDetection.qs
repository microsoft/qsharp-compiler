// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


// No Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Bar();
    }

    operation Bar() : Unit { }
}

// =================================

// Simple Cycle
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Bar();
    }

    operation Bar() : Unit {
        Foo();
    }
}

// =================================

// Longer Cycle
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Bar();
    }

    operation Bar() : Unit {
        Baz();
    }

    operation Baz() : Unit {
        Foo();
    }
}

// =================================

// Direct Recursion Cycle
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Foo();
    }
}

// =================================

// Loop In Sequence
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Bar();
    }

    operation Bar() : Unit {
        Bar();
        Baz();
    }

    operation Baz() : Unit { }
}

// =================================

// Figure-Eight Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Bar();
        Baz();
    }

    operation Bar() : Unit {
        Foo();
    }

    operation Baz() : Unit {
        Foo();
    }
}

// =================================

// Fully Connected Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo() : Unit {
        Foo();
        Bar();
        Baz();
    }

    operation Bar() : Unit {
        Foo();
        Bar();
        Baz();
    }

    operation Baz() : Unit {
        Foo();
        Bar();
        Baz();
    }
}

// =================================

// Sausage Link Graph Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation _1() : Unit {
        _2();
    }

    operation _2() : Unit {
        _1();
        _3();
    }

    operation _3() : Unit {
        _2();
        _4();
    }

    operation _4() : Unit {
        _3();
    }

}

// =================================

// Double Link Graph Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation _1() : Unit {
        _2();
        _4();
    }

    operation _2() : Unit {
        _6();
    }

    operation _3() : Unit {
        _1();
    }

    operation _4() : Unit {
        _6();
    }

    operation _5() : Unit {
        _1();
    }

    operation _6() : Unit {
        _5();
        _3();
    }

}

// =================================

// Multiple SCC Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation SCC1_1() : Unit {
        SCC1_1();
        SCC2_1();
    }

    operation SCC2_1() : Unit {
        SCC2_2();
        SCC3_1();
    }

    operation SCC2_2() : Unit {
        SCC2_1();
        SCC3_2();
    }

    operation SCC3_1() : Unit {
        SCC3_2();
    }

    operation SCC3_2() : Unit {
        SCC3_3();
    }

    operation SCC3_3() : Unit {
        SCC3_2();
        SCC3_1();
    }
}

// =================================

// Johnson's Graph Cycles
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation _1() : Unit {
        _2();
        _3();
        // ...
        _k1();
    }

    operation _2() : Unit {
        _k2();
    }

    operation _3() : Unit {
        _k2();
    }

    operation _k1() : Unit {
        _k2();
    }

    operation _k2() : Unit {
        _k3();
        _2k2();
    }

    operation _k3() : Unit {
        _2k();
        _2k2();
    }

    // ...

    operation _2k() : Unit {
        _2k1();
        _2k2();
    }

    operation _2k1() : Unit {
        _1();
        _2k2();
    }

    operation _2k2() : Unit {
        _2k3();
        _2k4();
        // ...
        _3k2();
    }

    operation _2k3() : Unit {
        _k2();
        _3k3();
    }

    operation _2k4() : Unit {
        _3k3();
    }

    operation _3k2() : Unit {
        _3k3();
    }

    operation _3k3() : Unit {
        _2k2();
    }
}
