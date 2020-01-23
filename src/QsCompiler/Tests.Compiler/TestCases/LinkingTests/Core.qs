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
}

namespace Microsoft.Quantum.Diagnostics {

    // needs to be available for testing
    @ Attribute()
    newtype Test = String;
}