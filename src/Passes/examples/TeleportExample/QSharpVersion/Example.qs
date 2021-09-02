namespace TeleportChain {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Preparation;  

//    @EntryPoint()
//    operation Main(): Int
//    {
//        use q = Qubit();    
//
//        mutable ret = 1;
//        for i in 0..5
//        {
//            set ret = ret + Calculate(4, q);
//        }
//
//        return ret;
//    }
//
//    operation Calculate(n: Int, c: Qubit): Int
//    {
//        use q = Qubit();        
//        mutable ret = 2;
//        
//        H(q);
//        CNOT(c,q);
//
//        if(n != 0)
//        {
//            set ret = Calculate(n - 1, q) + 2;
//        }
//
//        return ret;
//    }

//    open Microsoft.Quantum.Intrinsic;
//    open Microsoft.Quantum.Canon;
//    open Microsoft.Quantum.Arrays;
//    open Microsoft.Quantum.Measurement;
//    open Microsoft.Quantum.Preparation;
//
//
    operation PrepareEntangledPair(left : Qubit, right : Qubit) : Unit is Adj + Ctl {
        H(left);
        CNOT(left, right);
    }

    operation ApplyCorrection(src : Qubit, intermediary : Qubit, dest : Qubit) : Unit {
        if (MResetZ(src) == One) { Z(dest); }
        if (MResetZ(intermediary) == One) { X(dest); }
    }

    operation TeleportQubitUsingPresharedEntanglement(src : Qubit, intermediary : Qubit, dest : Qubit) : Unit {
        Adjoint PrepareEntangledPair(src, intermediary);
        ApplyCorrection(src, intermediary, dest);
    }

    @EntryPoint()
    operation DemonstrateTeleportationUsingPresharedEntanglement() : Result {
        let nPairs = 2;
        use (leftMessage, rightMessage, leftPreshared, rightPreshared) = (Qubit(), Qubit(), Qubit[nPairs], Qubit[nPairs]);
        PrepareEntangledPair(leftMessage, rightMessage);
        for i in 0..nPairs-1 {
            PrepareEntangledPair(leftPreshared[i], rightPreshared[i]);
        }

        TeleportQubitUsingPresharedEntanglement(rightMessage, leftPreshared[0], rightPreshared[0]);
        for i in 1..nPairs-1 {
            TeleportQubitUsingPresharedEntanglement(rightPreshared[i-1], leftPreshared[i], rightPreshared[i]);
        }
        
        let _ = MResetZ(leftMessage);
        return  MResetZ(rightPreshared[nPairs-1]);
    }


}