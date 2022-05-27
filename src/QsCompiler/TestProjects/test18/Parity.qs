namespace Test18 {
    open Microsoft.Quantum.Intrinsic;

    operation PrintParity(num: Int): Unit {
        let two = 2;
        let parity = num
%two
        ;
        Message($"Parity of {num} is {parity}");
    }
}
