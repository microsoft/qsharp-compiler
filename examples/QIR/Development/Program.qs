namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Samples;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.MachineLearning;

    newtype Foo = Int;
    newtype Register = (Data : Int[], Foo : Foo);


    
    // FIXME: DOESN'T WORK BECAUSE ASSIGNMENT TO IMMUTABLE
    // VARIABLE IS NOT REF COUNTED...
    function TestIssue (cond : Bool) : Unit {

        mutable arr = [[0], [0,0]];
        let value = arr;

        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestIssue2 (cond : Bool) : Unit {

        mutable arr = [[0], [0,0]];
        let value = arr[0];

        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestIssue1 (cond : Bool) : Unit {

        let value = [0];
        mutable arr = [value, [0,0]];


        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    // SAME TESTS JUST WITH INNER BLOCKS BEING CALLS?

    function TestUdt8 (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        if cond {
            mutable (value, data) = (Foo(1), [1, 1]);

            set reg w/= Foo <- value;
            set value = Foo(0);
            set reg w/= Foo <- value;

            set reg w/= Data <- data;
            set data w/= 1 <- 2;
            set reg w/= Data <- data;
        }

        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt7 (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        if cond {
            mutable (value, data) = (Foo(1), [1, 1]);

            set reg w/= Foo <- value;
            set value = Foo(0);
            set reg w/= Foo <- value;

            set reg w/= Data <- data;
            set data w/= 1 <- 2;
            set reg w/= Data <- data;
        }

        Message($"{reg}");
    }

    ///

    function TestUdt6 (cond : Bool) : Unit {
        let value = Foo(0);
        mutable reg = Register([], value);

        if cond {
            set reg w/= Foo <- Foo(0);
        }

        Message($"{value}");
        Message($"{reg}");
    }

    function TestUdt5 (cond : Bool) : Unit {
        let value = Foo(-1);

        let defaultVal = Register([], value);
        mutable reg = defaultVal;

        if cond {
            set reg w/= Foo <- Foo(0);
        }

        Message($"{value}");
        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt4 (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        let (value, data) = (Foo(0), [1,1]);
        if cond {
            set reg w/= Data <- data;
            set reg w/= Foo <- value;
        }

        Message($"{value}");
        Message($"{data}");
        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt3 (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        let (value, data) = (Foo(0), [1,1]);
        if cond {
            set reg w/= Data <- data;
            set reg w/= Foo <- value;
        }

        Message($"{value}");
        Message($"{data}");
        Message($"{reg}");
    }

    ///

    function TestUdt2 (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        if cond {
            set reg w/= Foo <- Foo(0);
            set reg w/= Data <- [1,2,3];
        }

        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt1 (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        if cond {
            set reg w/= Foo <- Foo(0);
            set reg w/= Data <- [1,2,3];
        }

        Message($"{reg}");
    }

    ///
    ///

    function TestArray8 () : Unit {
        let defaultArr = [Foo(-1), size = 3];
        mutable coeffs = defaultArr;

        for idx in 0 .. 1 {
            mutable value = Foo(idx);
            set coeffs w/= idx <- value;
            set value = Foo(0);
            set coeffs w/= idx + 1 <- value;
        }

        Message($"{defaultArr}");
        Message($"{coeffs}");
    }

    function TestArray7 () : Unit {
        mutable coeffs = [Foo(-1), size = 3];

        for idx in 0 .. 1 {
            mutable value = Foo(idx);
            set coeffs w/= idx <- value;
            set value = Foo(0);
            set coeffs w/= idx + 1 <- value;
        }

        Message($"{coeffs}");
    }

    ///

    function TestArray6 () : Unit {
        let value = Foo(0);
        mutable coeffs = [value, size = 3];

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

        Message($"{value}");
        Message($"{coeffs}");
    }

    function TestArray5 () : Unit {
        let value = Foo(0);

        let defaultArr = [value, size = 3];
        mutable coeffs = defaultArr;

        for idx in 0 .. 1 {
            set coeffs w/= idx <- value;
        }

        Message($"{value}");
        Message($"{defaultArr}");
        Message($"{coeffs}");
    }

    function TestArray4 () : Unit {
        let defaultArr = [Foo(-1), size = 3];
        mutable coeffs = defaultArr;

        let value = Foo(0);
        for idx in 0 .. 1 {
            set coeffs w/= idx <- value;
        }

        Message($"{value}");
        Message($"{defaultArr}");
        Message($"{coeffs}");
    }

    function TestArray3 () : Unit {
        mutable coeffs = [Foo(-1), size = 3];

        let value = Foo(0);
        for idx in 0 .. 1 {
            set coeffs w/= idx <- value;
        }

        Message($"{value}");
        Message($"{coeffs}");
    }

    ///

    function TestArray2 () : Unit {
        let defaultArr = [Foo(-1), size = 3];
        mutable coeffs = defaultArr;

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

        Message($"{defaultArr}");
        Message($"{coeffs}");
    }

    function TestArray1 () : Unit {
        mutable coeffs = [Foo(-1), size = 3];

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

        Message($"{coeffs}");
    }

    @EntryPoint()
    operation RunExample() : String {

        //let coefficients = [(1.0, 0.0), (1.0, 0.0), (1.0, 0.0)];
        ////mutable ret = coefficients;
        //mutable ret = [(1.0, 0.0), (1.0, 0.0), (1.0, 0.0)];
        //
        ////at this stage, alias and ref count of the original array is 2, and we //enter the loop with header__4
        //for idxNegative in 0 .. 1 {
        //
        //    let (mag, _) = coefficients[idxNegative];
        //    set ret w/= idxNegative <- (mag, 0.0);
        //}
        //
        //// loop 5 is an alias count decrease of coeffs
        //// loop 6 is an alias count decrease of current ret
        //// loop 7 and 8 are the corresponding ref count decreases

        //TestArray1();
        //TestArray2();
        //TestArray3();
        //TestArray4();
        //TestArray5();
        //TestArray6();
        //TestArray7();
        //TestArray8();
        //
        //TestUdt1(true);
        //TestUdt2(true);
        //TestUdt3(true);
        //TestUdt4(true);
        //TestUdt5(true);
        //TestUdt6(true);
        //TestUdt7(true);
        //TestUdt8(true);
        //TestUdt1(false);
        //TestUdt2(false);
        //TestUdt3(false);
        //TestUdt4(false);
        //TestUdt5(false);
        //TestUdt6(false);
        //TestUdt7(false);
        //TestUdt8(false);
        //
        TestIssue(true);
        TestIssue(false);
        return "Executed successfully!";
    }
}

namespace Microsoft.Quantum.Testing.QIR {

    newtype HasString = (
        Value: String,
        Data: Double[]
    );

    internal function Identity<'T>(input : 'T)
    : 'T {
        return input;
    }

    operation TestPendingRefCountIncreases(cond : Bool) : HasString {
        let s = HasString("<", [0.0]);
        let updated = s w/ Data <- [0.1, 0.2];
        if cond {
            return s;
        } else {
            return s;
        }
    }

    function TestRefCountsForItemUpdate(cond : Bool) : Unit {
        mutable ops = new Int[][5];
        if (cond) {
            set ops w/= 0 <- new Int[3];
        }
    }

    @EntryPoint()
    operation Main() : Unit {
        let _ = TestPendingRefCountIncreases(true);
        TestRefCountsForItemUpdate(true);

        let id = Identity;
        let _ = id(0, 0);
    }

}

namespace Microsoft.Quantum.Testing.QIR
{
    newtype Complex = (Re : Double, Im : Double);

    function TestArrayUpdate1(even : String) : String[]
    {
        mutable arr = new String[10];
        for i in 0 .. 9
        {
            let str = i % 2 != 0 ? "odd" | even;
            set arr w/= i <- str; 
        }

        return arr;
    }

    function TestArrayUpdate2(array : String[], even : String) : String[]
    {
        mutable arr = array;
        for i in 0 .. 9
        {
            let str = i % 2 != 0 ? "odd" | even;
            set arr w/= i <- str; 
        }

        return arr;
    }

    function TestArrayUpdate3(y : String[], b : String) : String[]
    {
        mutable x = y;
        set x w/= 0 <- b;
        set x w/= 1 <- "Hello";

        mutable arr = [(0,0), size = 10];
        for i in 0 .. 9
        {
            set arr w/= i <- (i, i + 1); 
        }

        return x;
    }

    function TestArrayUpdate4(array : String[]) : String[] 
    {
        let item = "Hello";
        mutable arr = new String[0];
        set arr = array;

        for i in 0 .. 9
        {
            set arr w/= i <- item; 
        }

        return arr;
    }

    function TestArrayUpdate5(cond : Bool, array : Complex[]) : Complex[]
    {
        let item = Complex(0.,0.);
        mutable arr = array;
        for i in 0 .. 2 .. Length(arr) - 1
        {
            set arr w/= i <- arr[i % 2]; 
            if (cond)
            {
                set arr w/= i % 2 <- item; 
            }
        }

        return arr;
    }

    @EntryPoint()
    function ArrayTest() : Unit {
        let _ = TestArrayUpdate1("");
        let _ = TestArrayUpdate2([], "");
        let _ = TestArrayUpdate3([], "");
        let _ = TestArrayUpdate4([]);
        let _ = TestArrayUpdate5(true, []);
    }
}


namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Diagnostics;

    newtype Options = (
        SimpleMessage: (String -> Unit),
        DumpToFile: (String -> Unit),
        DumpToConsole: (Unit -> Unit)
    );

    function Ignore<'T> (arg : 'T) : Unit {}

    function DefaultOptions() : Options {
        return Options(
            Ignore,
            Ignore,
            Ignore
        );
    }

    @EntryPoint()
    operation TestBuiltInIntrinsics() : Unit {
        let options = DefaultOptions()
            w/ SimpleMessage <- Message
            w/ DumpToFile <- DumpMachine
            w/ DumpToConsole <- DumpMachine;

        options::SimpleMessage("Hello");
        options::DumpToFile("pathToFile");
        options::DumpToConsole();
    }
}

namespace Microsoft.Quantum.Testing.QIR
{
    newtype Energy = (Abs : Double, Name : String);

    operation TestRepeat (q : Qubit) : Int
    {
        mutable n = 0;
        repeat
        {
            within
            {
                T(q);
            }
            apply
            {
                X(q);
            }
            H(q);

            let name = "energy";
            mutable res = Energy(0.0, "");
            set res w/= Name <- name;

            set n += 1;
        }
        until (M(q) == Zero)
        fixup
        {
            if (n > 100)
            {
                fail "Too many iterations";
            }

            set res w/= Name <- "";
        }
        return n;
    }

    @EntryPoint()
    operation Test0() : Unit {
        use q = Qubit();
        let _ = TestRepeat(q);
    }
}

namespace Microsoft.Quantum.Testing.QIR
{
    newtype TestType = ((Double, A : String), B : Int);
    newtype NamedValue = (Name : String, Value : Complex, Abs : Complex);

    function TestUdtUpdate1(a : String, b : Int) : TestType
    {
        mutable x = TestType((1.0, a), b);
        set x w/= A <- "Hello";
        return x;
    }

    function TestUdtUpdate2(cond : Bool, arg : NamedValue) : NamedValue
    {
        mutable namedValue = arg;
        if (arg::Name == "None")
        {
            set namedValue w/= Value <- Complex(0., 0.);
            if (cond)
            {
                set namedValue w/= Value <- Complex(1., 0.);
            }
            set namedValue w/= Abs <- namedValue::Value;
        }

        return namedValue;
    }

    @EntryPoint()
    function TestUdt() : Unit {
        let _ = TestUdtUpdate1("", 0);
        let _ = TestUdtUpdate2(true, NamedValue("", Complex(0.,0.), Complex(0.,0.)));
    }

}
