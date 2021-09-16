namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Intrinsic;

    newtype Foo = Int;
    newtype Register = (Data : Int[], Foo : Foo);

    // FIXME: ADD MORE HORRIBLE TEST CASES WHERE THE SECOND MUTABLE VARIABLE IS ALSO UPDATED VIA COPY-AND-REASSIGN...

    function TestIssue7(cond1 : Bool, cond2 : Bool, cond3 : Bool) : Unit {

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

    function TestIssue6(cond1 : Bool, cond2 : Bool) : Unit {

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

    function TestIssue5(cond : Bool) : Unit {
    
        mutable value = [[0], [0,0]];
        mutable arr = value;
        set value = [];
    
        if cond {
            set arr w/= 0 <- [];
        }
    
        Message($"{value}");
        Message($"{arr}");
    }

    function TestIssue4(cond : Bool) : Unit {

        mutable value = ["hello", "bye"];
        mutable arr = value;
        set value = [];

        if cond {
            set arr w/= 0 <- "";
        }

        Message($"{value}");
        Message($"{arr[0]}, {arr[1]}"); // FIXME: STRING[] PRINT IS INCORRECT
    }

    // TODO: tests for straight out variable update (not copy and update)

    // FIXME: ADD A TEST FOR CONDITIONAL EXPRESSIONS WHERE ONE ARRAY IS COPIED AND THE OTHER BRANCH IS NOT
    
    function TestIssue3 (cond : Bool) : Unit {

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
        
        TestIssue1(true);
        TestIssue1(false);
        TestIssue2(true);
        TestIssue2(false);
        TestIssue3(true);
        TestIssue3(false);

        TestIssue4(true);
        TestIssue4(false);
        TestIssue5(true);
        TestIssue5(false);
        TestIssue6(true, true);
        TestIssue6(false, true);
        TestIssue6(true, false);
        TestIssue6(false, false);
        TestIssue7(true, true, true);
        TestIssue7(false, true, true);
        TestIssue7(true, false, true);
        TestIssue7(false, false, true);
        TestIssue7(true, true, false);
        TestIssue7(false, true, false);
        TestIssue7(true, false, false);
        TestIssue7(false, false, false);

        return "Executed successfully!";
    }
}
