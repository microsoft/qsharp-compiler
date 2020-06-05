// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// Test cases for verification of execution target runtime capabilities.
namespace Microsoft.Quantum.Testing.CapabilityVerification {
    function ResultAsBool(result : Result) : Bool {
        return result == Zero ? false | true;
    }
}
