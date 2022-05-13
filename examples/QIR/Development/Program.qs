namespace Microsoft.Quantum.Qir.Development {
    open Microsoft.Quantum.Intrinsic;

    // @EntryPoint()
    // operation Main() : Unit {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     let rs = MeasureEachZ(qs);
    //     let val = ResultArrayAsInt(rs);
    //     Int_Record_Output(val);
    // }

    // Results in `alloca` in a middle block that won't get optimized.
    @EntryPoint()
    operation Main() : Unit {
        mutable val = 0;
        use coin = Qubit();
        H(coin);
        if M(coin) == One {
            use qs = Qubit[8];
            for q in qs {
                H(q);
            }
            let rs = MeasureEachZ(qs);
            set val = ResultArrayAsInt(rs);
        }
        Int_Record_Output(val);
    }

    // @EntryPoint()
    // operation Main() : Unit {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     mutable rs = [M(qs[0]), size = 8];
    //     for i in 1..Length(qs)-1 {
    //         set rs w/= i <- M(qs[i]);
    //     }
    //     // let val = ResultArrayAsInt(rs);
    //     // Int_Record_Output(val);
    //     Array_Start_Record_Output();
    //     for i in 0..Length(rs)-1 { // fails because compiler treats rs as NOT compile time known
    //     // for i in 0..Length(qs)-1 { // works because compiler treats qs as compile time known length
    //         Result_Record_Output(rs[i]);
    //     }
    //     Array_End_Record_Output();
    // }

    // @EntryPoint()
    // operation Main() : Unit {
    //     use qs = Qubit[2];
    //     for q in qs {
    //         H(q);
    //     }
    //     Int_Record_Output(MeasureIntoInt(qs));
    // }

    // @EntryPoint()
    // operation Main() : Unit {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     mutable rs = [];
    //     for q in qs {
    //         set rs = rs + [M(q)];
    //     }
    //     for i in 0..Length(qs)-1 {
    //         Result_Record_Output(rs[i]);
    //     }
    // }

    // @EntryPoint()
    // operation Main() : Unit {
    //     mutable rand = 0;
    //     use qs = Qubit[4];
    //     for q in qs {
    //         H(q);
    //         if M(q) == One {
    //         // if Measure([PauliX], [q]) == One {
    //             set rand += 1;
    //         }
    //         set rand <<<= 1;
    //     }
    //     Int_Record_Output(rand);
    // }

    // @EntryPoint()
    // operation Main() : Unit {
    //     // mutable array = [0, 0, 0, 0];
    //     mutable array = [0, size = 4];
    //     for a in array {
    //         Int_Record_Output(a);
    //     }
    //     Int_Record_Output(Length(array));
    // }

    operation MeasureEachZ(qs : Qubit[]) : Result[] {
        mutable rs = [M(qs[0]), size = Length(qs)];
        for i in 1..Length(qs)-1 {
            set rs = rs w/ i <- M(qs[i]);
        }
        return rs;
    }

    operation ResultArrayAsInt(rs : Result[]) : Int {
        mutable val = 0;
        for r in rs {
            if r == One {
                set val += 1;
            }
            set val <<<= 1;
        }
        return val;
    }

    operation MeasureIntoInt(qs : Qubit[]) : Int {
        mutable val = 0;
        for q in qs {
            if M(q) == One {
                set val += 1;
            }
            set val <<<= 1;
        }
        return val;
    }

    function Result_Record_Output(val : Result) : Unit {
        body intrinsic;
    }

    function Int_Record_Output(val : Int) : Unit {
        body intrinsic;
    }

    function Array_Start_Record_Output() : Unit {
        body intrinsic;
    }

    function Array_End_Record_Output() : Unit {
        body intrinsic;
    }

}

