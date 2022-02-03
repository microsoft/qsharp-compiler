namespace Microsoft.Quantum.Testing.ExecutionTests {

    open Microsoft.Quantum.Intrinsic;

    newtype Foo = (Value : Int);
    newtype Register = (Data : Int[], Foo : Foo);
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

    function TestUdt1a (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        if cond {
            set reg w/= Foo <- Foo(0);
            set reg w/= Data <- [1,2,3];
        }

        Message($"{reg}");
    }

    internal function TestUdtHelper1 (reg : Register) : Register {
        return reg
            w/ Foo <- Foo(0)
            w/ Data <- [1,2,3];
    }

    function TestUdt1b (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        if cond {
            set reg = TestUdtHelper1(reg);
        }

        Message($"{reg}");
    }

    function TestUdt2a (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        if cond {
            set reg w/= Foo <- Foo(0);
            set reg w/= Data <- [1,2,3];
        }

        Message($"{defaultVal}");
        Message($"{reg}");
    }

    internal function TestUdtHelper2 (reg : Register) : Register {
        return reg
            w/ Foo <- Foo(0)
            w/ Data <- [1,2,3];
    }

    function TestUdt2b (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        if cond {
            set reg = TestUdtHelper2(reg);
        }

        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt3a (cond : Bool) : Unit {
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

    internal function TestUdtHelper3 (reg : Register, value : Foo, data : Int[]) : Register {
        return reg
            w/ Data <- data
            w/ Foo <- value;
    }

    function TestUdt3b (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        let (value, data) = (Foo(0), [1,1]);
        if cond {
            set reg = TestUdtHelper3(reg, value, data);
        }

        Message($"{value}");
        Message($"{data}");
        Message($"{reg}");
    }

    function TestUdt4a (cond : Bool) : Unit {
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

    internal function TestUdtHelper4 (reg : Register, value : Foo, data : Int[]) : Register {
        return reg
            w/ Data <- data
            w/ Foo <- value;
    }

    function TestUdt4b (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        let (value, data) = (Foo(0), [1,1]);
        if cond {
            set reg = TestUdtHelper4(reg, value, data);
        }

        Message($"{value}");
        Message($"{data}");
        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt5a (cond : Bool) : Unit {
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

    internal function TestUdtHelper5 (reg : Register) : Register {
        return reg w/ Foo <- Foo(0);
    }

    function TestUdt5b (cond : Bool) : Unit {
        let value = Foo(-1);

        let defaultVal = Register([], value);
        mutable reg = defaultVal;

        if cond {
            set reg = TestUdtHelper5(reg);
        }

        Message($"{value}");
        Message($"{defaultVal}");
        Message($"{reg}");
    }

    function TestUdt6a (cond : Bool) : Unit {
        let value = Foo(0);
        mutable reg = Register([], value);

        if cond {
            set reg w/= Foo <- Foo(0);
        }

        Message($"{value}");
        Message($"{reg}");
    }

    internal function TestUdtHelper6 (reg : Register) : Register {
        return reg w/ Foo <- Foo(0);
    }

    function TestUdt6b (cond : Bool) : Unit {
        let value = Foo(0);
        mutable reg = Register([], value);

        if cond {
            set reg = TestUdtHelper6(reg);
        }

        Message($"{value}");
        Message($"{reg}");
    }

    function TestUdt7a (cond : Bool) : Unit {
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

    internal function TestUdtHelper7 (reg : Register) : Register {
        mutable (value, data) = (Foo(1), [1, 1]);
        set value = Foo(0);
        set data w/= 1 <- 2;

        return reg
            w/ Foo <- value
            w/ Data <- data;
    }

    function TestUdt7b (cond : Bool) : Unit {
        mutable reg = Register([], Foo(-1));

        if cond {
            set reg = TestUdtHelper7(reg);
        }

        Message($"{reg}");
    }

    function TestUdt8a (cond : Bool) : Unit {
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

    internal function TestUdtHelper8 (reg : Register) : Register {
        mutable (value, data) = (Foo(1), [1, 1]);
        set value = Foo(0);
        set data w/= 1 <- 2;

        return reg
            w/ Foo <- value
            w/ Data <- data;
    }

    function TestUdt8b (cond : Bool) : Unit {
        let defaultVal = Register([], Foo(-1));
        mutable reg = defaultVal;

        if cond {
            set reg = TestUdtHelper8(reg);
        }

        Message($"{defaultVal}");
        Message($"{reg}");
    }


    function TestArray1 () : Unit {
        mutable coeffs = [Foo(-1), size = 3];

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

        Message($"{coeffs}");
    }

    function TestArray2 () : Unit {
        let defaultArr = [Foo(-1), size = 3];
        mutable coeffs = defaultArr;

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

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

    function TestArray6 () : Unit {
        let value = Foo(0);
        mutable coeffs = [value, size = 3];

        for idx in 0 .. 1 {
            set coeffs w/= idx <- Foo(idx);
        }

        Message($"{value}");
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

    function TestArrayUpdate1a (cond : Bool) : Unit {

        let value = [0];
        mutable arr = [value, [0,0]];


        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate1b (cond : Bool) : Unit {

        let value = [0];
        mutable arr = [value, [0,0]];


        if cond {
            set arr w/= 1..-1..0 <- arr;
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate2a (cond : Bool) : Unit {

        mutable arr = [[0], [0,0]];
        let value = arr[0];

        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate2b (cond : Bool) : Unit {

        mutable arr = [[0], [0,0], []];
        let value = arr[0];

        if cond {
            set arr w/= 0..2..2 <- [[1], [2]];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate3a (cond : Bool) : Unit {

        mutable arr = [[0], [0,0]];
        let value = arr;

        if cond {
            set arr w/= 0 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate3b (cond : Bool) : Unit {

        mutable arr = [[0], [0,0]];
        let value = arr;

        if cond {
            set arr w/= 0..-1 <- [];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate4a (cond : Bool) : Unit {
        mutable arr = [[0], [0,0]];
        let value = arr;

        set arr = cond
            ? [[0], size = 5] w/ 3 <- [1]
            | (value w/ 1 <- []);

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate4b (cond : Bool) : Unit {
        mutable arr = [[0], [0,0], []];
        let value = arr;

        set arr = cond
            ? [[0], size = 5] w/ 4..-2..-1 <- [[], size = 3]
            | (value w/ 0..2..3 <- arr[1...]);

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate5a(cond : Bool) : Unit {

        mutable value = ["hello", "bye"];
        mutable arr = value;
        set value = [];

        if cond {
            set arr w/= 0 <- "";
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate5b(cond : Bool) : Unit {

        mutable value = ["hello", "bye"];
        mutable arr = value;
        set value = [""];

        if cond {
            set arr w/= 0 <- "";
            set value w/= 0 <- arr[1];
        }

        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate6a(cond : Bool) : Unit {
    
        mutable value = [[0], [0,0]];
        mutable arr = value;
        set value = [];
    
        if cond {
            set arr w/= 0 <- [];
        }
    
        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate6b(cond : Bool) : Unit {
    
        mutable value = [[0], [0,0]];
        mutable arr = value;
        set value = [[]];
    
        if cond {
            let item = [1,2];
            set arr w/= 0 <- [];
            set value w/= 0 <- (item w/ 1 <- 0);
        }
    
        Message($"{value}");
        Message($"{arr}");
    }

    function TestArrayUpdate7a(cond1 : Bool, cond2 : Bool) : Unit {

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

    function TestArrayUpdate7b(cond1 : Bool, cond2 : Bool) : Unit {

        mutable value = [[0], [0,0]];
        if cond1 {
            mutable arr = value;
            set value = [[],[]];

            if cond2 {
                set arr w/= 0 <- [];
                set value w/= 1 <- [1];
            }

            Message($"{arr}");

        }
        Message($"{value}");
    }


    function TestArrayUpdate8a(cond1 : Bool, cond2 : Bool, cond3 : Bool) : Unit {

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

    function TestArrayUpdate8b(cond1 : Bool, cond2 : Bool, cond3 : Bool) : Unit {

        mutable value = [[0], [0,0]];
        if cond1 {
            mutable arr = value;
            if cond2 {
                set value = [[1],[2]];

                if cond2 {
                    set arr w/= 0 <- [];
                    set value w/= 1 <- (value[1] w/ 0 <- 0);
                }
            }
            Message($"{arr}");
        }
        Message($"{value}");
    }


    function TestCopyAndUpdate1 () : Unit {

        let udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4") w/ Item1 <- "";
        Message($"{udt}");
    }

    function TestCopyAndUpdate2 () : Unit {

        let udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4") w/ Item2 <- "";
        Message($"{udt}");
    }

    function TestCopyAndUpdate3 () : Unit {

        let udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4") w/ Item3 <- Tuple("", "");
        Message($"{udt}");
    }

    function TestCopyAndUpdate4 () : Unit {

        let udt = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4") w/ Item4 <- "";
        Message($"{udt}");
    }

    function TestCopyAndUpdate5a () : Unit {

        let original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item1 <- "";
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate5b () : Unit {

        mutable original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item1 <- "";
        set original w/= Item4 <- "_";
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate6a () : Unit {

        let original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item2 <- "";
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate6b () : Unit {

        mutable original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item2 <- "";
        set original w/= Item3 <- Tuple("_", "_");
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate7a () : Unit {

        let original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item3 <- Tuple("", "");
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate7b () : Unit {

        mutable original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item3 <- Tuple("", "");
        set original w/= Item2 <- original::Item2;
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate8a () : Unit {

        let original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item4 <- "";
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate8b () : Unit {

        mutable original = MyUdt(("s1", ("s2", Tuple("s3a", "s3b"))), "s4");
        let udt = original w/ Item4 <- "";
        set original w/= Item1 <- udt::Item1;
        Message($"{original}");
        Message($"{udt}");
    }

    function TestCopyAndUpdate9a () : Unit {

        let arr = [[0, 1, 2], size = 5] w/ 3 <- [];
        Message($"{arr}");
    }

    function TestCopyAndUpdate9b () : Unit {

        mutable original = [[0, 1, 2], size = 5];
        let arr = original w/ 3 <- [];
        set original w/= 0 <- [0];
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate10a () : Unit {

        let original = [[0, 1, 2], size = 5];
        let arr = original w/ 3 <- [];
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate10b () : Unit {

        mutable original = [[0, 1, 2], size = 5];
        let arr = original w/ 3 <- [];
        set original w/= 0 <- original[0];
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate11a () : Unit {

        let original = [[0, 1, 2], size = 5];
        let arr = original w/ 3 <- (original[3] w/ 1 <- -1);
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate11b () : Unit {

        mutable original = [[0, 1, 2], size = 5];
        let arr = original w/ 3 <- (original[3] w/ 1 <- -1);
        set original w/= 0 <- original[1];
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate12a () : Unit {

        let item = [0, 1, 2];
        let original = [item, size = 5];
        let arr = original w/ 3 <- (original[3] w/ 1 <- -1);
        Message($"{item}");
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate12b () : Unit {

        let item = [0, 1, 2];
        mutable original = [item, size = 5];
        let arr = original w/ 3 <- (original[3] w/ 1 <- -1);
        set original w/= 0 <- arr[0];
        Message($"{item}");
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate13a () : Unit {

        let item = [0, 1, 2];
        let original = [item, size = 5];
        let arr = original w/ 3 <- (item w/ 1 <- -1);
        Message($"{item}");
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate13b () : Unit {

        mutable item = [0, 1, 2];
        let original = [item, size = 5];
        let arr = original w/ 3 <- (item w/ 1 <- -1);
        set item w/= 0 <- -1;
        Message($"{item}");
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate14a () : Unit {

        let original = [[0, 1, 2], size = 5];
        let arr = original[3] w/ 1 <- -1;
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate14b () : Unit {

        mutable original = [[0, 1, 2], size = 5];
        let arr = original[3] w/ 1 <- -1;
        set original w/= 3 <- [];
        Message($"{original}");
        Message($"{arr}");
    }

    function GetArrayOfArray(rows : Int, columns : Int) : String[][] {
        return [["", size = columns], size = rows];
    }

    function TestCopyAndUpdate15a () : Unit {
        let arr = GetArrayOfArray(3,4) w/ 1 <- [];
        Message($"{arr}");
    }

    function TestCopyAndUpdate15b () : Unit {
        Message($"{GetArrayOfArray(3,4) w/ 1 <- []}");
    }

    function TestCopyAndUpdate16a () : Unit {

        mutable original = [[0, 1, 2], size = 5];
        let arr = original w/ 0..2..2 <- [[-1],[-1]];
        set original w/= 1..2 <- [[10], [10]];
        Message($"{original}");
        Message($"{arr}");
    }

    function TestCopyAndUpdate16b () : Unit {

        let item = [1];
        mutable arr = [[0, 1, 2], size = 5] w/ 0..2..2 <- [item, item];
        set arr w/= 1..2 <- [[10], [10]];
        Message($"{item}");
        Message($"{arr}");
    }

    function TestVariableReassignment1 () : Unit {

        mutable (foo, rep) = (Foo(1), 1);
        while rep < 3 {
            set (foo, rep) = (Foo(foo::Value + 1), rep + 1);
        }

        Message($"{foo::Value}");
    }

    function TestVariableReassignment2 () : Unit {

        mutable foo = "";
        for i in 1 .. 5 {
            set foo += $"{i},";
        }
        Message(foo);
    }

    function TestVariableReassignment3 () : Unit {

        mutable foo = Tuple("iter", "0");
        for i in 1 .. 5 {
            set foo = Tuple(foo::Item1, foo::Item2 + $",{i}");
        }
        Message($"{foo}");
    }

    operation TestVariableReassignment4 () : Unit {

        mutable (foo, rep) = (Foo(1), "");
        repeat {
            set foo = Foo(foo::Value + 1);
        }
        until (rep == "   ")
        fixup
        {
            set rep += " ";
        }

        Message($"{foo::Value}");
    }

    operation TestVariableReassignment5 () : Unit {

        mutable (foo, rep) = (Foo(1), "");
        repeat {
            set foo = Foo(foo::Value + 1);
            set rep += " ";
        }
        until (rep == "   ");

        Message($"{foo}");
    }

    operation TestVariableReassignment6 () : Unit {

        mutable foo = "";
        use q = Qubit(){
            set foo = "updated";
        }
        Message(foo);
    }

    operation TestVariableReassignment7 () : Unit {

        mutable foo = "";
        use q = Qubit();
        set foo = "reset";
        Message(foo);
    }

    function TestVariableReassignment8 (str : String, value : String) : Unit {

        mutable foo = "";
        if str == "a" {
            set foo = "a";
        }
        elif str == "b" {
            set foo = "b";
        }
        elif str == "c" {
            set foo = str;
        }
        elif str == "d" {
            set foo = value;
        }
        else {
            set foo += "unknown";
        }
        Message(foo);
    }


    @EntryPoint()
    operation TestDefaultValues() : Unit {
        Message($"{new Int[3]}");
        Message($"{new Double[3]}");
        Message($"{new Bool[3]}");
        Message($"{new Pauli[3]}");
        Message($"{new String[3]}");
        Message($"{new Range[3]}");
        Message($"{new Qubit[3]}");
        Message($"{new Result[3]}");
        Message($"{new Unit[3]}");
        // Todo: bigint is not yet supported in the runtime,
        // see also https://github.com/microsoft/qsharp-runtime/issues/910
        Message($"{new (String, Qubit)[3]}");
        Message($"{new String[][3]}");
        Message($"{new MyUdt[3]}");
        Message($"{new (Qubit[] -> Unit)[3]}");
        Message($"{new (Qubit[] => Unit is Adj + Ctl)[3]}");
    }


    @EntryPoint()
    operation TestArraySlicing() : Range {

        mutable arr = [1,2,3,4];
        Message($"{arr}, {arr[...-1...]}");
        mutable value = arr;
        let check1 = value;
        mutable check2 = arr;

        if arr[0] == 1 {
            set arr w/= 3..-1..0 <- arr;
            Message($"{arr}, {value}");
            set value w/= 0..2..2 <- arr[Length(arr)/2...];
            Message($"{arr}, {value}");
            set arr w/= 0..-1 <- [];
            Message($"{arr}, {value}");
        }

        mutable arrarr = [[], [0], [1]];
        set arrarr w/= 2..-1..0 <- arrarr;
        let iter = [[1],[2],[3]];
        Message($"{arrarr}, {iter w/ 2..-1..0 <- iter}");

        set arrarr = value[0] == 1
            ? [[10], size = 5] w/ 4..-2..-1 <- [[6], size = 3]
            | (iter w/ 0..2..3 <- arrarr[1...]);
        Message($"{arrarr}, {iter}");

        set arrarr = value[0] != 1
            ? [[10], size = 5] w/ 4..-2..-1 <- [[5], size = 3]
            | (iter w/ 0..2..3 <- arrarr[1...]);
        Message($"{arrarr}, {iter}");

        set arrarr = [[0], [0,0], [1,1,1]];
        Message($"{arrarr}");
        set arrarr w/= 1..-1..0 <- arrarr[0..1];
        Message($"{arrarr}");
        set arrarr w/= 2..-1..0 <- arrarr;
        Message($"{arrarr}");
        set arrarr w/= 0..2..3 <- arrarr[0..2..3][...-1...];
        Message($"{arrarr}");

        Message($"{check1}, {check2}");
        return 1..3..5;
    }

    internal function PrintSection(sectionIdx : Int, sectionTitle : String) : Unit {
        Message("\n********************");
        Message($"Section {sectionIdx}: {sectionTitle}");
        Message("********************\n");
    }

    @EntryPoint()
    operation TestInterpolatedStrings() : String {

        let arr1 = [1,2,3];
        let arr2 = ["1","2","3"];
        let arr3 = ["","2","","4"];
        let tuple1 = (1,(2,3));
        let tuple2 = ("1",("2","3"));
        let tuple3 = ("1",("","3"));
        let res = One;
        let pauli = PauliX;
        let pauliArr = [PauliY, PauliI];
        let range = 0..-1..0;
        let (fct, op) = (PrintSection, RunExample);
        let udt = Tuple("Hello", "World");
        
        Message("simple string");
        Message($"{"interpolated string"}");
        // Todo: bigint is not yet supported in the runtime,
        // see also https://github.com/microsoft/qsharp-runtime/issues/910
        Message($"{true} or {false}, {res == Zero ? false | true}, {res == One ? false | true}, {res == One ? true | false}, {res == Zero ? true | false}");
        Message($"{1}, {-1}, {1 - 1}");
        Message($"{1.}, {2.0}, {1e5}, {.1}, {-1.}, {1. - 1.}");
        Message($"{Zero}, {res}");
        Message($"{PauliZ}, {pauli}, {pauliArr[0]}, {pauliArr[1...]}");
        Message($"{1..3}, {3..-1..1}, {range}");
        Message($"{[1,2,3]}, {["1","2","3"]}, {arr1}, {arr2}, {arr3}");
        Message($"{()}, {(1,(2,3))}, {("1",("2","3"))}, {tuple1}, {tuple2}, {tuple3}");
        
        use (q, qs) = (Qubit(), Qubit[3]);
        Message($"{q}, {qs}");
        Message($"{PrintSection}, {RunExample}, {(fct, op)}");
        Message($"{udt::Item1}, {Foo(1)}, {udt}");
        return "All good!";
    }

    @EntryPoint()
    operation NoReturn() : Unit {}

    @EntryPoint()
    operation ReturnsUnit() : Unit {
        return ();
    }

    @EntryPoint()
    operation ReturnsString() : String {
        return "Success!";
    }

    @EntryPoint()
    operation RunExample() : Unit {

        PrintSection(1, "");
        
        TestUdtUpdate1();
        TestUdtUpdate2();
        TestUdtUpdate3();
        TestUdtUpdate4();
        TestUdtUpdate5();
        TestUdtUpdate6();
        TestUdtUpdate7();
        TestUdtUpdate8();
        
        PrintSection(2, "");
        
        TestUdtUpdate1b();
        TestUdtUpdate2b();
        TestUdtUpdate3b();
        TestUdtUpdate4b();
        TestUdtUpdate5b();
        TestUdtUpdate6b();
        TestUdtUpdate7b();
        TestUdtUpdate8b();
        
        PrintSection(3, "");
        
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
        
        PrintSection(4, "");
        
        TestUdtUpdate9();
        TestUdtUpdate10();
        TestUdtUpdate11();
        TestUdtUpdate12();
        TestUdtUpdate13();
        TestUdtUpdate14();
        TestUdtUpdate15();
        TestUdtUpdate16();
        
        PrintSection(5, "");
        
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
        
        PrintSection(6, "");
        
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
        
        PrintSection(7, "");
        
        TestUdtUpdate17();
        TestUdtUpdate18();
        TestUdtUpdate19();
        TestUdtUpdate20();
        TestUdtUpdate21();
        TestUdtUpdate22();
        TestUdtUpdate23();
        TestUdtUpdate24();
        
        PrintSection(8, "");
        
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
        
        PrintSection(9, "");
        
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
        
        PrintSection(10, "");
        
        TestUdt1a(true);
        TestUdt2a(true);
        TestUdt3a(true);
        TestUdt4a(true);
        TestUdt5a(true);
        TestUdt6a(true);
        TestUdt7a(true);
        TestUdt8a(true);
        
        TestUdt1a(false);
        TestUdt2a(false);
        TestUdt3a(false);
        TestUdt4a(false);
        TestUdt5a(false);
        TestUdt6a(false);
        TestUdt7a(false);
        TestUdt8a(false);
        
        PrintSection(11, "");
        
        TestUdt1b(true);
        TestUdt2b(true);
        TestUdt3b(true);
        TestUdt4b(true);
        TestUdt5b(true);
        TestUdt6b(true);
        TestUdt7b(true);
        TestUdt8b(true);
        
        TestUdt1b(false);
        TestUdt2b(false);
        TestUdt3b(false);
        TestUdt4b(false);
        TestUdt5b(false);
        TestUdt6b(false);
        TestUdt7b(false);
        TestUdt8b(false);
        
        PrintSection(12, "");
        
        TestArray1();
        TestArray2();
        TestArray3();
        TestArray4();
        TestArray5();
        TestArray6();
        TestArray7();
        TestArray8();
        
        PrintSection(13, "");
        
        TestArrayUpdate1a(true);
        TestArrayUpdate1a(false);
        TestArrayUpdate2a(true);
        TestArrayUpdate2a(false);
        TestArrayUpdate3a(true);
        TestArrayUpdate3a(false);
        TestArrayUpdate4a(true);
        TestArrayUpdate4a(false);

        TestArrayUpdate1b(true);
        TestArrayUpdate1b(false);
        TestArrayUpdate2b(true);
        TestArrayUpdate2b(false);
        TestArrayUpdate3b(true);
        TestArrayUpdate3b(false);
        TestArrayUpdate4b(true);
        TestArrayUpdate4b(false);

        PrintSection(14, "");

        TestArrayUpdate5a(true);
        TestArrayUpdate5a(false);
        TestArrayUpdate6a(true);
        TestArrayUpdate6a(false);
        TestArrayUpdate7a(true, true);
        TestArrayUpdate7a(false, true);
        TestArrayUpdate7a(true, false);
        TestArrayUpdate7a(false, false);
        TestArrayUpdate8a(true, true, true);
        TestArrayUpdate8a(false, true, true);
        TestArrayUpdate8a(true, false, true);
        TestArrayUpdate8a(false, false, true);
        TestArrayUpdate8a(true, true, false);
        TestArrayUpdate8a(false, true, false);
        TestArrayUpdate8a(true, false, false);
        TestArrayUpdate8a(false, false, false);

        PrintSection(15, "");

        TestArrayUpdate5b(true);
        TestArrayUpdate5b(false);
        TestArrayUpdate6b(true);
        TestArrayUpdate6b(false);
        TestArrayUpdate7b(true, true);
        TestArrayUpdate7b(false, true);
        TestArrayUpdate7b(true, false);
        TestArrayUpdate7b(false, false);
        TestArrayUpdate8b(true, true, true);
        TestArrayUpdate8b(false, true, true);
        TestArrayUpdate8b(true, false, true);
        TestArrayUpdate8b(false, false, true);
        TestArrayUpdate8b(true, true, false);
        TestArrayUpdate8b(false, true, false);
        TestArrayUpdate8b(true, false, false);
        TestArrayUpdate8b(false, false, false);

        PrintSection(16, "");

        TestCopyAndUpdate1();
        TestCopyAndUpdate2();
        TestCopyAndUpdate3();
        TestCopyAndUpdate4();
        TestCopyAndUpdate5a();
        TestCopyAndUpdate6a();
        TestCopyAndUpdate7a();
        TestCopyAndUpdate8a();
        TestCopyAndUpdate5b();
        TestCopyAndUpdate6b();
        TestCopyAndUpdate7b();
        TestCopyAndUpdate8b();

        PrintSection(17, "");

        TestCopyAndUpdate9a();
        TestCopyAndUpdate10a();
        TestCopyAndUpdate11a();
        TestCopyAndUpdate12a();
        TestCopyAndUpdate13a();
        TestCopyAndUpdate14a();
        TestCopyAndUpdate15a();
        TestCopyAndUpdate16a();

        TestCopyAndUpdate9b();
        TestCopyAndUpdate10b();
        TestCopyAndUpdate11b();
        TestCopyAndUpdate12b();
        TestCopyAndUpdate13b();
        TestCopyAndUpdate14b();
        TestCopyAndUpdate15b();
        TestCopyAndUpdate16b();

        PrintSection(18, "");

        TestVariableReassignment1();
        TestVariableReassignment2();
        TestVariableReassignment3();
        TestVariableReassignment4();
        TestVariableReassignment5();
        TestVariableReassignment6();
        TestVariableReassignment7();
        TestVariableReassignment8("a", "?");
        TestVariableReassignment8("b", "?");
        TestVariableReassignment8("c", "?");
        TestVariableReassignment8("d", "?");
        TestVariableReassignment8("e", "?");

        Message("Executed successfully!");
    }

    @EntryPoint()
    function CheckFail() : Unit {
        fail "expected failure in CheckFail";
    }
}

namespace Microsoft.Quantum.Intrinsic {

    function Message (arg : String) : Unit {
        body intrinsic;
    }
}
