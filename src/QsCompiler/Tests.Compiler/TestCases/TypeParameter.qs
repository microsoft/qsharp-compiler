// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// Partial Resolution
namespace Microsoft.Quantum.Testing.TypeParameter {

    operation Main() : Unit {
        (Foo(_, 3, "Hi"))(1.0);
    }

    operation Foo<'A, 'B, 'C>(a : 'A, b : 'B, c : 'C) : Unit { }
}

// =================================

