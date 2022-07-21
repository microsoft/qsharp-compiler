namespace Test18 {
    // TODO: Uncomment this once references are fixed for notebooks
    //open Microsoft.Quantum.Intrinsic;

    operation PrintParity(num: Int): Unit {
        let two = 2;
        let parity = num
%two
        ;
        // TODO: Uncomment this once references are fixed for notebooks
        //Message($"Parity of {num} is {parity}");
    }

    newtype Nested = (Double, (ItemName : Int, String));
}
