// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// This namespace contains test cases for expression and statement verification
namespace Microsoft.Quantum.Testing.LocalVerification {

    operation StringParsingTest1 () : Unit {
        let str = "";
    }

    operation StringParsingTest2 () : Unit {
        let str = "Hello";
    }

    operation StringParsingTest3 () : Unit {
        let str = "\"";
    }

    operation StringParsingTest4 () : Unit {
        let str = ";";
    }

    operation StringParsingTest5 () : Unit {
        let str = "$";
    }

    operation StringParsingTest6 () : Unit {
        let str = "Hello"; // "
    }

    operation StringParsingTest7 () : Unit {
        let str = "//";
    }

    operation MultiLineStringTest1 () : Unit {
        let str = "
        ";
    }

    operation MultiLineStringTest2 () : Unit {
        let str = "
        Hello
        ";
    }

    operation MultiLineStringTest3 () : Unit {
        let str = "
        \"
        ";
    }

    operation MultiLineStringTest4 () : Unit {
        let str = "
        ;
        ";
    }

    operation MultiLineStringTest5 () : Unit {
        let str = "
        $
        ";
    }

    operation MultiLineStringTest6 () : Unit {
        let str = "
        $";
    }

    operation MultiLineStringTest7 () : Unit {
        let str = "
        Hello
        "; // "
    }

    operation MultiLineStringTest8 () : Unit {
        let str = "
        // \"
        ";
    }

    operation MultiLineStringTest9 () : Unit { // This should error
        let str = "
        // "
        ";
    }

//"} // This comment helps prevent parsing problems in previous tests from affecting the rest of the code.

}
