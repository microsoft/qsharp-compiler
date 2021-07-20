// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    operation GetRandomBool(seed : Int) : Bool {
        body intrinsic;
    }

    @EntryPoint()
    operation TestShortCircuiting () : (Bool, Bool) {
        let rand = GetRandomBool(1) and GetRandomBool(2);
        let ror = GetRandomBool(3) or GetRandomBool(4);
        return (rand, ror);        
    }
}
