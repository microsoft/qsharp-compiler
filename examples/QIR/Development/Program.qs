namespace Microsoft.Quantum.Qir.Development {
    open Microsoft.Quantum.Intrinsic;

    // Returning mutables and using them to calculate output.
    @EntryPoint()
    operation Main() : Int {
        use qs = Qubit[8];
        for q in qs {
            H(q);
        }
        let rs = MeasureEachZ(qs);
        let val = ResultArrayAsInt(rs);
        return val;
    }

    // // Calculating mutables in a quantum conditional
    // @EntryPoint()
    // operation Main() : Int {
    //     mutable val = 0;
    //     use coin = Qubit();
    //     H(coin);
    //     if M(coin) == One {
    //         use qs = Qubit[8];
    //         for q in qs {
    //             H(q);
    //         }
    //         let rs = MeasureEachZ(qs);
    //         set val = ResultArrayAsInt(rs);
    //     }
    //     return val;
    // }

    // // Returning mutable array built with copy update.
    // @EntryPoint()
    // operation Main() : Result[] {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     mutable rs = [M(qs[0]), size = 8];
    //     for i in 1..Length(qs)-1 {
    //         set rs w/= i <- M(qs[i]);
    //     }
    //     return rs;
    // }

    // // Returning mutable array built with copy update and mutable int in struct.
    // @EntryPoint()
    // operation Main() : (Result[], Int) {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     mutable rs = [M(qs[0]), size = 8];
    //     for i in 1..Length(qs)-1 {
    //         set rs w/= i <- M(qs[i]);
    //     }
    //     return (rs, ResultArrayAsInt(rs));
    // }

    // // Build up result array using concatenation on a mutable
    // @EntryPoint()
    // operation Main() : Result[] {
    //     use qs = Qubit[8];
    //     for q in qs {
    //         H(q);
    //     }
    //     mutable rs = [];
    //     for q in qs {
    //         set rs = rs + [M(q)];
    //     }
    //     return rs;
    // }
    // // Fails with "Value does not fall within expected range" at
    // // LlvmBindings.Instructions.InstructionBuilder.Store(Value value, Value destination) in C:\Users\swern\Programming\qsharp-compiler\src\QsCompiler\LlvmBindings\Instructions\InstructionBuilder.cs:line 371

    // // Use target package implementation of Measure
    // @EntryPoint()
    // operation Main() : Int {
    //     mutable rand = 0;
    //     use qs = Qubit[8];
    //     for q in qs {
    //         if Measure([PauliX], [q]) == One {
    //             set rand += 1;
    //         }
    //         set rand <<<= 1;
    //     }
    //     return rand;
    // }

    // @EntryPoint()
    // operation Main() : (Result, Result[]) {
    //     use q = Qubit();
    //     return (Zero, []);
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

}

