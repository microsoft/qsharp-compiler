// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// =================================

// Return Int
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int {
        return 0;
    }
}

// =================================

// Return Bool
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Bool {
        return false;
    }
}

// =================================

// Return Double
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Double {
        return 0.0;
    }
}

// =================================

// Return Result
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Result {
        return Zero;
    }
}

// =================================

// Return Tuple
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : (Int, Bool) {
        return (0, false);
    }
}

// =================================

// Return Nested Tuple
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : (Int, (Bool, Double)) {
        return (0, (false, 0.0));
    }
}

// =================================

// Return Array
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int[] {
        return [2, 4, 6, 8];
    }
}

// =================================

// Return Empty Array
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int[] {
        return [];
    }
}

// =================================

// Return Jagged Array
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int[][] {
        return [[0], [1, 2], [3, 4, 5]];
    }
}

// =================================

// Return Array in Tuple
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : (Result, Int[]) {
        return (Zero, [1, 3, 5]);
    }
}

// =================================

// Multiple Parameter Entry Point
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo(a : Int, b : Bool) : Int {
        return 0;
    }
}

// =================================

// Multiple Entry Points
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int {
        return 0;
    }

    @EntryPoint()
    operation Bar() : Bool {
        return false;
    }
}

// =================================

// Don't Wrap Unit Entry Points
namespace Microsoft.Quantum.Testing.OutputRecording {

    @EntryPoint()
    operation Foo() : Int {
        return 0;
    }

    @EntryPoint()
    operation Bar() : Unit { }
}
