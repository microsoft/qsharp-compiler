// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    @EntryPoint()
    operation TestDeepNesting(r1 : Result, r2 : Result, r3 : Result) : Int {
        if r1 == r2 {
            if r2 == r3 {
                if r3 == r1 {
                    if r1 == One {
                        if r2 == One {
                            if r3 == One {
                                if r3 != Zero {
                                    if r2 != Zero {
                                        return 1;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return 0;
    }
}
