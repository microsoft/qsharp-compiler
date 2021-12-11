namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Int {
        let var_x = 42;
        mutable var_y = IntToInt(var_x);
        set var_y = ToInt();
        return var_y;
    }

    operation IntToInt(var_x: Int) : Int {
        return var_x + 1;
    }

    operation ToInt() : Int {
        return 26;
    }
}
