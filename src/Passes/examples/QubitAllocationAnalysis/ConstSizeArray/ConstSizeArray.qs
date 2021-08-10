namespace TeleportChain {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Preparation;

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

    operation TeleportQubit(src : Qubit, dest : Qubit) : Unit {
        use intermediary = Qubit();
        PrepareEntangledPair(intermediary, dest);
        TeleportQubitUsingPresharedEntanglement(src, intermediary, dest);
    }

    operation DemonstrateEntanglementSwapping() : (Result, Result) {
        use (reference, src, intermediary, dest) = (Qubit(), Qubit(), Qubit(), Qubit());
        PrepareEntangledPair(reference, src);
        TeleportQubit(src, dest);
        return (MResetZ(reference), MResetZ(dest));
    }

    @EntryPoint()
    operation DemonstrateTeleportationUsingPresharedEntanglement() : (Result, Result) {
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

        return (MResetZ(leftMessage), MResetZ(rightPreshared[nPairs-1]));
    }
}