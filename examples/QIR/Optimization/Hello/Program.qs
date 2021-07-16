namespace Hello {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    
    @EntryPoint()
    operation HelloQ() : Unit {
        Message("Hello quantum world!");
    }
}
