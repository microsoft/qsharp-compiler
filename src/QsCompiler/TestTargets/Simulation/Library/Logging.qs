// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.Simulation
{
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;

    /// # Summary
    /// Logs the given message of the given severity. 
    function Log (severity : Int, msg : String) : Unit {
        let intro = 
            severity >= 3 ? "Error: " 
            | severity == 2 ? "Warning: " 
            | severity == 1 ? "Info: " 
            | "";
        Message($"{intro}{msg}");
    }
}
