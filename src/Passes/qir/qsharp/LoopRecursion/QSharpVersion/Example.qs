namespace TeleportChain {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Preparation;  

    @EntryPoint()
    operation QuantumProgram(): Int
    {
        use q = Qubit();    

        mutable ret = 1;
        for i in 0..5
        {
            set ret = ret + Calculate(4, q);
        }

        return ret;
    }

    operation Calculate(n: Int, c: Qubit): Int
    {
        use q = Qubit();        
        mutable ret = 2;
        
        H(q);
        CNOT(c,q);

        if(n != 0)
        {
            set ret = Calculate(n - 1, q) + 2;
        }

        return ret;
    }

}