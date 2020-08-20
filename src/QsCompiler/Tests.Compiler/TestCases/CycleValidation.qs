// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


// Cycle with Generic Resolution
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A>() : Unit {
        Bar<'A>();
    }

    operation Bar<'A>() : Unit {
        Baz<'A>();
    }

    operation Baz<'A>() : Unit {
        Foo<'A>();
    }
}

// =================================

// Cycle with Concrete Resolution
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B,'C>() : Unit {
        Bar<Int,'B,'C>();
    }

    operation Bar<'A,'B,'C>() : Unit {
        Baz<'A,Double,'C>();
    }

    operation Baz<'A,'B,'C>() : Unit {
        Foo<'A,'B,String>();
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

// Cycle with Multiple Concrete Resolutions
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A>() : Unit {
        Bar<Int>();
    }

    operation Bar<'A>() : Unit {
        Baz<Double>();
    }

    operation Baz<'A>() : Unit {
        Foo<'A>();
    }
}

// =================================

// Cycle with Rotating Constriction
namespace Microsoft.Quantum.Testing.CycleDetection {

    operation Foo<'A,'B,'C>() : Unit {
        Bar<'C,'A,'B>();
        Bar<'B,'C,'A>();
    }

    operation Bar<'A,'B,'C>() : Unit {
        Baz<'C,'A,'B>();
        Baz<'B,'C,'A>();
    }

    operation Baz<'A,'B,'C>() : Unit {
        Foo<'C,'A,'B>();
        Foo<'B,'C,'A>();
    }
}
