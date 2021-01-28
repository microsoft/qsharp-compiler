namespace Quantum.App1 {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    
    @EntryPoint()
    operation GetRandomResult() : Result {
        use q = Qubit();
        H(q);
        return M(q);
    }
}

