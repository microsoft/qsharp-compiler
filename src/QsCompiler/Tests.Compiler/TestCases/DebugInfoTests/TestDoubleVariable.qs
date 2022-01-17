namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Double {
        let varX = 42.1;
        mutable varY = 43.3;
        set varY = varX;
        return varY;
    }
}
