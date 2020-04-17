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