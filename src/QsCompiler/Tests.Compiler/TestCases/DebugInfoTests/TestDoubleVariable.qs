namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Double {
        let var_x = 42.1;
        mutable var_y = 43.3;
        set var_y = var_x;
        return var_y;
    }
}
