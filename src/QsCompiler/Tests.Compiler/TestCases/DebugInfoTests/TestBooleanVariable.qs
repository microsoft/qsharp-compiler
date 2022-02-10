namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Bool {
        let varX = true;
        mutable varY = false;
        set varY = varX;
        return varY;
    }
}
