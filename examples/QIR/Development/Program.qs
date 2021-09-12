namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.MachineLearning;

    newtype Foo = Int;
    newtype Register = (Data : Int[], Foo : Foo);

    // FIXME: ADD MORE HORRIBLE TEST CASES WHERE THE SECOND MUTABLE VARIABLE IS ALSO UPDATED VIA COPY-AND-REASSIGN...

    function HorribleCase3(cond1 : Bool, cond2 : Bool, cond3 : Bool) : Unit {

        mutable value = [[0], [0,0]];
        if cond1 {
            mutable arr = value;
            if cond2 {
                set value = [];

                if cond2 {
                    set arr w/= 0 <- [];
                }
            }
            Message($"{arr}");
        }
        Message($"{value}");
    }

    function HorribleCase2(cond1 : Bool, cond2 : Bool) : Unit {

        mutable value = [[0], [0,0]];
        if cond1 {
            mutable arr = value;
            set value = [];

            if cond2 {
                set arr w/= 0 <- [];
            }

            Message($"{arr}");

        }
        Message($"{value}");
    }

    //function HorribleCase1(cond : Bool) : Unit {
    //
    //    mutable value = [[0], [0,0]];
    //    mutable arr = value;
    //    set value = [];
    //
    //    if cond {
    //        set arr w/= 0 <- [];
    //    }
    //
    //    Message($"{value}");
    //    Message($"{arr}");
    //}

    function HorribleCase1(cond : Bool) : Unit {

        mutable value = ["hello", "bye"];
        mutable arr = value;
        // orig array literal has now alias count 2 and ref count 2
        set value = [];
        // the empty array has alias count 1 and ref count 1
        // orig array literal has alias count 1, ref count 2

        if cond {
            set arr w/= 0 <- "";
        }

        Message($"{value}");
        Message($"{arr[0]}, {arr[1]}"); // FIXME: STRING[] PRINT IS INCORRECT
    }

    // TODO: tests for straight out variable update (not copy and update)

    // FIXME: ADD A TEST FOR CONDITIONAL EXPRESSIONS WHERE ONE ARRAY IS COPIED AND THE OTHER BRANCH IS NOT
    
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

    newtype Tuple = (Item1 : String, Item2 : String);
    newtype MyUdt = ((Item1: String, (Item2 : String, Item3 : Tuple)), Item4 : String);

    function TestUdtUpdate1 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item1 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate1a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            set udt w/= Item1 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate1b () : Unit {

        let value = "_";
        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item1 <- value;
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate2 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item2 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate2a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            set udt w/= Item2 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate2b () : Unit {

        let value = "_";
        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item2 <- value;
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate3 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
    }

    function TestUdtUpdate3a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            set udt w/= Item3 <- Tuple("_", "_");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate3b () : Unit {

        let value = Tuple("_", "_");
        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item3 <- value;
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate4 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item4 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate4a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            set udt w/= Item4 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate4b () : Unit {

        let value = "_";
        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        set udt w/= Item4 <- value;
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate5 () : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        set udt w/= Item1 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate5a (cond : Bool) : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        if cond {
            set udt w/= Item1 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate5b () : Unit {

        let value = "_";
        let s = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(s, "s3b"))), "s4");
        set udt w/= Item1 <- value;
        Message($"{udt}");
        Message($"{value}, {s}");
    }

    function TestUdtUpdate6 () : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        set udt w/= Item2 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate6a (cond : Bool) : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        if cond {
            set udt w/= Item2 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate6b () : Unit {

        let value = "_";
        let s = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(s, "s3b"))), "s4");
        set udt w/= Item2 <- value;
        Message($"{udt}");
        Message($"{value}, {s}");
    }

    function TestUdtUpdate7 () : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
    }

    function TestUdtUpdate7a (cond : Bool) : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        if cond {
            set udt w/= Item3 <- Tuple("_", "_");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate7b () : Unit {

        let value = Tuple("_", "_");
        let s = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(s, "s3b"))), "s4");
        set udt w/= Item3 <- value;
        Message($"{udt}");
        Message($"{value}, {s}");
    }

    function TestUdtUpdate8 () : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        set udt w/= Item4 <- "_";
        Message($"{udt}");
    }

    function TestUdtUpdate8a (cond : Bool) : Unit {

        let value = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(value, "s3b"))), "s4");
        if cond {
            set udt w/= Item4 <- "_";
        }
        Message($"{udt}");
    }

    function TestUdtUpdate8b () : Unit {

        let value = "_";
        let s = " ";
        mutable udt = MyUdt(("s1", ("s2", Tuple(s, "s3b"))), "s4");
        set udt w/= Item4 <- value;
        Message($"{udt}");
        Message($"{value}, {s}");
    }

    //

    function TestUdtUpdate9 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        set udt w/= Item1 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate9a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        if cond {
            set udt w/= Item1 <- udt::Item4;
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate9b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            mutable orig = udt;
            set orig w/= Item1 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate10 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        set udt w/= Item2 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate10a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        if cond {
            set udt w/= Item2 <- udt::Item4;
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate10b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            mutable orig = udt;
            set orig w/= Item2 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate11 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate11a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        if cond {
            mutable value = Tuple("_", "_");
            set udt w/= Item3 <- value;
            set value w/= Item1 <- "";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate11b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            mutable orig = udt;
            set orig w/= Item3 <- Tuple("_", "_");
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate12 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        set udt w/= Item4 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate12a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        mutable orig = udt;
        if cond {
            set udt w/= Item4 <- udt::Item1;
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate12b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            mutable orig = udt;
            set orig w/= Item4 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    //

    function TestUdtUpdate13 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        set udt w/= Item1 <- "_";
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate13a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        if cond {
            set udt w/= Item1 <- "_";
        }
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate13b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        if cond {
            let value = udt::Item3;
            set udt w/= Item1 <- "_";
            Message($"{value}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate14 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        set udt w/= Item2 <- "_";
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate14a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        if cond {
            set udt w/= Item2 <- "_";
        }
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate14b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        if cond {
            let value = udt::Item3;
            set udt w/= Item2 <- "_";
            Message($"{value}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate15 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate15a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        if cond {
            set udt w/= Item3 <- Tuple("_", "_");
        }
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate15b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        if cond {
            let value = udt::Item3;
            set udt w/= Item3 <- Tuple("_", "_");
            Message($"{value}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate16 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        set udt w/= Item4 <- "_";
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate16a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        let value = udt::Item3;
        if cond {
            set udt w/= Item4 <- "_";
        }
        Message($"{udt}");
        Message($"{value}");
    }

    function TestUdtUpdate16b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("3a", "s3b"))), "s4");
        if cond {
            let value = udt::Item3;
            set udt w/= Item4 <- "_";
            Message($"{value}");
        }
        Message($"{udt}");
    }

    //

    function TestUdtUpdate17 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        set udt w/= Item1 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate17a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        if cond {
            set udt w/= Item1 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate17b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let orig = udt::Item2;
            set udt w/= Item1 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate18 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        set udt w/= Item2 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate18a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        if cond {
            set udt w/= Item2 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate18b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let orig = udt::Item2;
            set udt w/= Item2 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate19 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate19a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        if cond {
            set udt w/= Item3 <- Tuple("_", "_");
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate19b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let orig = udt::Item2;
            set udt w/= Item3 <- Tuple("_", "_");
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate20 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        set udt w/= Item4 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate20a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let orig = udt::Item2;
        if cond {
            set udt w/= Item4 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate20b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let orig = udt::Item2;
            set udt w/= Item4 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate21 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        set udt w/= Item1 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate21a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        if cond {
            set udt w/= Item1 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate21b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let ((_, orig), _) = udt!;
            set udt w/= Item1 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate22 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        set udt w/= Item2 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate22a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        if cond {
            set udt w/= Item2 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate22b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let ((_, orig), _) = udt!;
            set udt w/= Item2 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate23 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        set udt w/= Item3 <- Tuple("_", "_");
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate23a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        if cond {
            set udt w/= Item3 <- Tuple("_", "_");
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate23b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let ((_, orig), _) = udt!;
            set udt w/= Item3 <- Tuple("_", "_");
            Message($"{orig}");
        }
        Message($"{udt}");
    }

    function TestUdtUpdate24 () : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        set udt w/= Item4 <- "_";
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate24a (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let ((_, orig), _) = udt!;
        if cond {
            set udt w/= Item4 <- "_";
        }
        Message($"{udt}");
        Message($"{orig}");
    }

    function TestUdtUpdate24b (cond : Bool) : Unit {

        mutable udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        if cond {
            let ((_, orig), _) = udt!;
            set udt w/= Item4 <- "_";
            Message($"{orig}");
        }
        Message($"{udt}");
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

        TestUdtUpdate1();
        TestUdtUpdate2();
        TestUdtUpdate3();
        TestUdtUpdate4();
        TestUdtUpdate5();
        TestUdtUpdate6();
        TestUdtUpdate7();
        TestUdtUpdate8();

        TestUdtUpdate1b();
        TestUdtUpdate2b();
        TestUdtUpdate3b();
        TestUdtUpdate4b();
        TestUdtUpdate5b();
        TestUdtUpdate6b();
        TestUdtUpdate7b();
        TestUdtUpdate8b();

        TestUdtUpdate1a(true);
        TestUdtUpdate2a(true);
        TestUdtUpdate3a(true);
        TestUdtUpdate4a(true);
        TestUdtUpdate5a(true);
        TestUdtUpdate6a(true);
        TestUdtUpdate7a(true);
        TestUdtUpdate8a(true);

        TestUdtUpdate1a(false);
        TestUdtUpdate2a(false);
        TestUdtUpdate3a(false);
        TestUdtUpdate4a(false);
        TestUdtUpdate5a(false);
        TestUdtUpdate6a(false);
        TestUdtUpdate7a(false);
        TestUdtUpdate8a(false);

        TestUdtUpdate9();
        TestUdtUpdate10();
        TestUdtUpdate11();
        TestUdtUpdate12();
        TestUdtUpdate13();
        TestUdtUpdate14();
        TestUdtUpdate15();
        TestUdtUpdate16();

        TestUdtUpdate9a(true);
        TestUdtUpdate10a(true);
        TestUdtUpdate11a(true);
        TestUdtUpdate12a(true);
        TestUdtUpdate13a(true);
        TestUdtUpdate14a(true);
        TestUdtUpdate15a(true);
        TestUdtUpdate16a(true);

        TestUdtUpdate9a(false);
        TestUdtUpdate10a(false);
        TestUdtUpdate11a(false);
        TestUdtUpdate12a(false);
        TestUdtUpdate13a(false);
        TestUdtUpdate14a(false);
        TestUdtUpdate15a(false);
        TestUdtUpdate16a(false);

        TestUdtUpdate9b(true);
        TestUdtUpdate10b(true);
        TestUdtUpdate11b(true);
        TestUdtUpdate12b(true);
        TestUdtUpdate13b(true);
        TestUdtUpdate14b(true);
        TestUdtUpdate15b(true);
        TestUdtUpdate16b(true);

        TestUdtUpdate9b(false);
        TestUdtUpdate10b(false);
        TestUdtUpdate11b(false);
        TestUdtUpdate12b(false);
        TestUdtUpdate13b(false);
        TestUdtUpdate14b(false);
        TestUdtUpdate15b(false);
        TestUdtUpdate16b(false);

        TestUdtUpdate17();
        TestUdtUpdate18();
        TestUdtUpdate19();
        TestUdtUpdate20();
        TestUdtUpdate21();
        TestUdtUpdate22();
        TestUdtUpdate23();
        TestUdtUpdate24();

        TestUdtUpdate17a(true);
        TestUdtUpdate18a(true);
        TestUdtUpdate19a(true);
        TestUdtUpdate20a(true);
        TestUdtUpdate21a(true);
        TestUdtUpdate22a(true);
        TestUdtUpdate23a(true);
        TestUdtUpdate24a(true);

        TestUdtUpdate17a(false);
        TestUdtUpdate18a(false);
        TestUdtUpdate19a(false);
        TestUdtUpdate20a(false);
        TestUdtUpdate21a(false);
        TestUdtUpdate22a(false);
        TestUdtUpdate23a(false);
        TestUdtUpdate24a(false);

        TestUdtUpdate17b(true);
        TestUdtUpdate18b(true);
        TestUdtUpdate19b(true);
        TestUdtUpdate20b(true);
        TestUdtUpdate21b(true);
        TestUdtUpdate22b(true);
        TestUdtUpdate23b(true);
        TestUdtUpdate24b(true);

        TestUdtUpdate17b(false);
        TestUdtUpdate18b(false);
        TestUdtUpdate19b(false);
        TestUdtUpdate20b(false);
        TestUdtUpdate21b(false);
        TestUdtUpdate22b(false);
        TestUdtUpdate23b(false);
        TestUdtUpdate24b(false);

        TestArray1();
        TestArray2();
        TestArray3();
        TestArray4();
        TestArray5();
        TestArray6();
        TestArray7();
        TestArray8();
        
        TestUdt1(true);
        TestUdt2(true);
        TestUdt3(true);
        TestUdt4(true);
        TestUdt5(true);
        TestUdt6(true);
        TestUdt7(true);
        TestUdt8(true);
        TestUdt1(false);
        TestUdt2(false);
        TestUdt3(false);
        TestUdt4(false);
        TestUdt5(false);
        TestUdt6(false);
        TestUdt7(false);
        TestUdt8(false);
        
        TestIssue(true);
        TestIssue(false);
        HorribleCase1(true);
        HorribleCase1(false);
        HorribleCase2(true, true);
        HorribleCase2(false, true);
        HorribleCase2(true, false);
        HorribleCase2(false, false);
        HorribleCase3(true, true, true);
        HorribleCase3(false, true, true);
        HorribleCase3(true, false, true);
        HorribleCase3(false, false, true);
        HorribleCase3(true, true, false);
        HorribleCase3(false, true, false);
        HorribleCase3(true, false, false);
        HorribleCase3(false, false, false);
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
