namespace TeleportChain {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Preparation;  


    function Value(r: Result): Int
    {
         return r == Zero ? 122 | 1337;
    }

    @EntryPoint()
    operation Main(): Int
    {
        let nrIter = 5;
        mutable ret = 1;
        for _ in 1 .. nrIter {
            use q = Qubit();
            H(q);
            let r = MResetZ(q);
            set ret = Value(r);
        }

        return ret;
    }

}