// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// This namespace contains test cases for expression and statement verification
namespace Microsoft.Quantum.Testing.LocalVerification {

    operation StringInterpolationTest1 () : Unit {
        let x = 0;
        let str = $"{x}";
    }

    operation StringInterpolationTest2 () : Unit {
        let x = 0;
        let str = $"{
            x
        }";
    }

    operation StringInterpolationSimpleStringTest1 () : Unit {
        let str = $"{"Hello"}";
    }

    operation StringInterpolationSimpleStringTest2 () : Unit {
        let str = $"{
            "Hello"
        }";
    }

    operation StringInterpolationSimpleStringTest3 () : Unit {
        let str = $"{"
            Hello
        "}";
    }

    operation StringInterpolationSimpleStringTest4 () : Unit {
        let str = $"{
            "
                Hello
            "
        }";
    }

    operation StringInterpolationQuoteTest1 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{" \" "}";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationQuoteTest2 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                "\""
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.

    operation StringInterpolationQuoteTest3 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{"
                \"
            "}";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationQuoteTest4 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                "
                    \"
                "
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    // ToDo: Make the semicolon tests fail harder
    operation StringInterpolationSemicolonTest1 () : Unit {
        let str = $"{";"}";
    }
    
    operation StringInterpolationSemicolonTest2 () : Unit {
        let str = $"{
            ";"
        }";
    }
    
    operation StringInterpolationSemicolonTest3 () : Unit {
        let str = $"{"
            ;
        "}";
    }
    
    operation StringInterpolationSemicolonTest4 () : Unit {
        let str = $"{
            "
                ;
            "
        }";
    }
    
    operation StringInterpolationDollarSignTest1 () : Unit {
        let str = $"{"$"}";
    }
    
    operation StringInterpolationDollarSignTest2 () : Unit {
        let str = $"{
            "$"
        }";
    }
    
    operation StringInterpolationDollarSignTest3 () : Unit {
        let str = $"{"
            $
        "}";
    }
    
     operation StringInterpolationDollarSignTest4 () : Unit {
        let str = $"{"
            $"
        }";
    }
    
    operation StringInterpolationDollarSignTest5 () : Unit {
        let str = $"{
            "
                $
            "
        }";
    }
    
    operation StringInterpolationDollarSignTest6 () : Unit {
        let str = $"{
            "
                $"
        }";
    }

    operation StringInterpolationOpenBraceTest1 () : Unit {
        let str = $"{"{"}";
    }
    
    operation StringInterpolationOpenBraceTest2 () : Unit {
        let str = $"{
            "{"
        }";
    }
    
    operation StringInterpolationOpenBraceTest3 () : Unit {
        let str = $"{"
            {
        "}";
    }
    
     operation StringInterpolationOpenBraceTest4 () : Unit {
        let str = $"{"
            {"
        }";
    }
    
    operation StringInterpolationOpenBraceTest5 () : Unit {
        let str = $"{
            "
                {
            "
        }";
    }
    
    operation StringInterpolationOpenBraceTest6 () : Unit {
        let str = $"{
            "
                {"
        }";
    }
    
    operation StringNestedInterpolationTest1 () : Unit {
        let x = 0;
        let str = $"Outer { $"Inner {x}" }";
    }
    
    operation StringNestedInterpolationTest2 () : Unit {
        let x = 0;
        let str = $"Outer {
            $"Inner {
                x
            }"
        }";
    }
    
    operation StringNestedInterpolationTest3 () : Unit {
        let x = 0;
        let str = $"Outer
        {
            $"Inner
            {
                x
            }
            "
        }
        ";
    }
    
    operation StringInterpolationWithCommentTest1 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{//;
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest2 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{//"
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest3 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{//}
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest4 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{//}"
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest5 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                //;
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest6 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                //"
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest7 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                //}
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
    
    operation StringInterpolationWithCommentTest8 () : Unit {
        use q = Qubit() { // This provides an extra scope for the test to close if it is erroneous.
            let str = $"{
                //}"
                "Hello"
            }";
        }
    }
    
//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.
}
