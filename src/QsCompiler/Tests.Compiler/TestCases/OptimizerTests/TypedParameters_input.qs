// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for optimization dealing with typed parameters
namespace Microsoft.Quantum.Arrays {
    function Length<'T>(array : 'T[]) : Int {
        body intrinsic;
    }

    function IndexRange<'T>(array : 'T[]) : Range {
        return 0..Length(array) - 1;
    }
}
