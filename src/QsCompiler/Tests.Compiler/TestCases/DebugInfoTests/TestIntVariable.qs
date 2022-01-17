namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Int {
        let varX = 42;
        mutable varY = 43;
        set varY = varX;
        return varY;
    }
}
