namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Int {
        let var_x = 42;
        mutable var_y = 43;
        set var_y = var_x;
        return var_y;
    }
}
