// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Tests.LineNumbers {
    
    open Microsoft.Quantum.Intrinsic;
    
    
    operation TestLineInBlocks (n : Int) : Result {
        
        let r = n + 1;
        
        using ((ctrls, q) = (Qubit[r], Qubit())) {
            
            if (n == 0) {
                X(q);
            }
            else {
                
                for (c in ctrls[0 .. 2 .. r]) {
                    Controlled X([c], q);
                }
            }
        }
        
        return Zero;
    }
    
}


