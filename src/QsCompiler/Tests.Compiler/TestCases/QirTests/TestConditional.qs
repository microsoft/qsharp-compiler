// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR{

    newtype CustomTuple = (Data : Double[]);

    function DrawRandom(arg : Double[]) : Int {
        body intrinsic;
    }

    function ReturnInt(arg : CustomTuple) : Int {
        if DrawRandom(arg::Data) < 0 {
            return 1;
        }
        else {
            return 0;
        }    
    }

    function ReturnFromNested(branch1 : Bool, branch2 : Bool) : Int {
        if branch1 {
            if branch2 {
                return 1;
            }
            else {
                return 2;
            }
        }
        else {
            if branch2 {
                return 3;
            }
            else {
                return 4;
            }
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
        let _ = ReturnFromNested(true, false);
        let result = ReturnInt(CustomTuple([3.]));
        return result;
    }
}
