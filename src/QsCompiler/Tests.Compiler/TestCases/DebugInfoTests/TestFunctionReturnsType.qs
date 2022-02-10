namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Int {
        let varX = 42;
        mutable varY = IntToInt(varX);
        set varY = ToInt();
        return varY;
    }

    operation IntToInt(varX: Int) : Int {
        return varX + 1;
    }

    operation ToInt() : Int {
        return 26;
    }
}
