// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.ExecutionTests
{    
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Testing;


    operation PackageAndProjectReference () : Unit {
        Message("Welcome to Q#!");
        Log(1, "Go check out https://docs.microsoft.com/en-us/quantum/?view=qsharp-preview.");
    }
}
