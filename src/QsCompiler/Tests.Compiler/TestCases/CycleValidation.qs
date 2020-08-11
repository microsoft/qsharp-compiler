// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


// Cycle with Generic Resolution
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B>() : Unit {
        Bar<'A,'B>();
    }

    operation Bar<'A,'B>() : Unit {
        Baz<'A,'B>();
    }

    operation Baz<'A,'B>() : Unit {
        Foo<'A,'B>();
    }
}

// =================================

// Cycle with Concrete Resolution
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B>() : Unit {
        Bar<'A,'B>();
    }

    operation Bar<'A,'B>() : Unit {
        Baz<Int,'B>();
    }

    operation Baz<'A,'B>() : Unit {
        Foo<'A,Double>();
    }
}

// =================================

// Constricting Cycle
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B>() : Unit {
        Bar<'A,'B>();
    }

    operation Bar<'A,'B>() : Unit {
        Baz<'A,'A>();
    }

    operation Baz<'A,'B>() : Unit {
        Foo<'A,'B>();
    }
}

// =================================

// Cycle with Rotating Parameters
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B,'C>() : Unit {
        Bar<'C,'A,'B>();
    }

    operation Bar<'A,'B,'C>() : Unit {
        Baz<'C,'A,'B>();
    }

    operation Baz<'A,'B,'C>() : Unit {
        Foo<'C,'A,'B>();
    }
}

// =================================

// Cycle with Mutated Forwarding
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A>() : Unit {
        Bar<'A>();
    }

    operation Bar<'A>() : Unit {
        Baz<'A[]>();
    }

    operation Baz<'A>() : Unit {
        Foo<'A>();
    }
}

// =================================

// Verify Cycle
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B>() : Unit {
        Bar<'A,'B>();
    }

    operation Bar<'A,'B>() : Unit {
        Baz<Int,'B>();
    }

    operation Baz<'A,'B>() : Unit {
        Zip<'B,'A>();
    }

    operation Zip<'A,'B>() : Unit {
        Zap<Double,'B>();
    }

    operation Zap<'A,'B>() : Unit {
        Foo<'A,'B>();
    }
}
