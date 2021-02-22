// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR{

    newtype CustomTuple = (Data : Double[]);

    function DrawRandom(arg : Double[]) : Int {
        body intrinsic;
    }

    function ReturnInt(arg : CustomTuple) : Int {
        if (DrawRandom(arg::Data) < 0) {
            return 1;
        }
        else {
            return 0;
        }    
    }

    function Hello(withPunctuation : Bool) : String[] {
       let arr = ["Hello","World", ""];
       return withPunctuation ?
           arr | (arr w/ 2 <- "!");
    }

    @EntryPoint()
    operation TestConditional (arg : Double[]) : Int {
        let _ = Hello(true);
        let result = ReturnInt(CustomTuple([3.]));
        return result;
    }
}
