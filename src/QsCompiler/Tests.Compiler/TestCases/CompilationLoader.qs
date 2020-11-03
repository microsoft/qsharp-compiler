// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Each test case is separated by "// ---". The text immediately after "// ---" until the end of the line is the name of
// the test case, which usually corresponds to a test name in CompilationLoaderTests.fs.

// --- Control Flow

namespace Microsoft.Quantum.Testing.CompilationLoader {

    function IsDivisibleByThreeOrFiveOrSeven(i: Int) : (Bool, Bool, Bool) {
        mutable isDivisibleByThree = false;
        mutable isDivisibleByFive = false;
        mutable isDivisibleByEleven = false;
        if ((i % 3) == 0) {
            set isDivisibleByThree = true;
        }
        elif ((i % 5) == 0){
            set isDivisibleByFive = true;
        }
        else {
            set isDivisibleByEleven = (i % 7) == 0;
        }

        return (isDivisibleByThree, isDivisibleByFive, isDivisibleByEleven);
    }

    @ EntryPoint()
    operation Main() : Unit {
        let (isDivisibleByThree, _, _) = IsDivisibleByThreeOrFiveOrSeven(3);
        let (_, isDivisibleByFive, _) = IsDivisibleByThreeOrFiveOrSeven(5);
        let (_, _, isDivisibleBySeven) = IsDivisibleByThreeOrFiveOrSeven(7);
    }
}

// --- Declarations

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        let zeros = new Int[10];
        let emptyRegister = new Qubit[0];
        let arr = [10, 11, 36, 49];
        let ten = arr[0];
        let odds = arr[1..2..4];

    }
}

// --- Functions

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        Foo();
        Bar();
    }
    
    function Foo() : Unit { }
    
    function Bar() : Unit {
        Baz();
    }
    
    function Baz() : Unit { }
}

// --- Loops

namespace Microsoft.Quantum.Testing.CompilationLoader {
    function ForLoop() : Unit {
        mutable counter = 0;
        for (i in 1 .. 2 .. 10) {
            set counter = counter + 1;
        }
    }

    function WhileLoop() : Unit {
        let arr = [-10, -11, 36, -49];
        mutable (item, index) = (-1, 0);
        while (index < Length(arr) && item < 0) { 
            set item = arr[index];
            set index += 1;
        }
    }

    @ EntryPoint()
    operation Main() : Unit {
        ForLoop();
        WhileLoop();
    }
}

// --- Mutable

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        mutable aMutableBoolean = false;
        set aMutableBoolean = true;
    }
}

// --- Operations

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        Foo();
        Bar();
    }
    
    operation Foo() : Unit { }
    
    operation Bar() : Unit {
        Baz();
    }
    
    operation Baz() : Unit { }
}

// --- Qubit Usage

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        using (qubit = Qubit()) {}
    }
}

// --- Tuple Deconstruction

namespace Microsoft.Quantum.Testing.CompilationLoader {
    @ EntryPoint()
    operation Main() : Unit {
        let (i, f) = (5, 0.1); // i is bound to 5 and f to 0.1
        mutable (a, (_, b)) = (1, (2, 3)); // a is bound to 1, b is bound to 3
        mutable (x, y) = ((1, 2), [3, 4]); // x is bound to (1,2), y is bound to [3,4]
        set (x, _, y) = ((5, 6), 7, [8]);  // x is rebound to (5,6), y is rebound to [8]
    }
}

// --- Update And Reassign

namespace Microsoft.Quantum.Testing.CompilationLoader {
    newtype Complex = (Re : Double, Im : Double);

    function ComplexSum(reals : Double[], ims : Double[]) : Complex {
        mutable res = Complex(0.0,0.0);

        for (r in reals) {
            set res w/= Re <- res::Re + r; // update-and-reassign statement
        }
        for (i in ims) {
            set res w/= Im <- res::Im + i; // update-and-reassign statement
        }
        return res;
    }

    @ EntryPoint()
    operation Main() : Unit {
        let reals = [10.0, 0.0, 11.0];
        let imaginaries = [0.0, 10.0, 11.0];
        let sum = ComplexSum(reals, imaginaries);
    }
}

// --- User Defined Types

namespace Microsoft.Quantum.Testing.CompilationLoader {
    newtype Complex = (Re : Double, Im : Double);

    function ComplexAddition(c1 : Complex, c2 : Complex) : Complex {
        return Complex(c1::Re + c2::Re, c1::Im + c2::Im);
    }

    @ EntryPoint()
    operation Main() : Unit {
        let realUnit = Complex(1.0, 0.0);
        let imaginaryUnit = Complex(0.0, 1.0);
        let realPlusImaginary = ComplexAddition(realUnit, imaginaryUnit);
    }
}
