namespace Quantum.$safeprojectname$ {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    
    @EntryPoint()
    operation HelloQ() : Result {
        use q = Qubit();
        H(q);
        return M(q);
    }
}

