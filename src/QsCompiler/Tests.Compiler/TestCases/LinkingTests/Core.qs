// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// needs to be available for testing
namespace Microsoft.Quantum.Core {

    @ Attribute()
    newtype Attribute = Unit;

    @ Attribute()
    newtype EntryPoint = Unit;

    @ Attribute()
    newtype Deprecated = String;


    function Default<'T>() : 'T {
        return (new 'T[1])[0];
    }

    function Length<'T>(arr : 'T[]) : Int {
        body intrinsic;
    }

    function RangeReverse(r : Range) : Range {
        body intrinsic;
    }
}

namespace Microsoft.Quantum.Diagnostics {

    // needs to be available for testing
    @ Attribute()
    newtype Test = String;
}

namespace Microsoft.Quantum.Arrays {

    // needs to be available for testing
    function IndexRange<'TElement>(arr : 'TElement[]) : Range {
        return 0 .. Length(arr) - 1;
    }
}

