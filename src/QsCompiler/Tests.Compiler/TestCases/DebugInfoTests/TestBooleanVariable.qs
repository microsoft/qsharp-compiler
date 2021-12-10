namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Bool {
        let var_x = true;
        mutable var_y = false;
        set var_y = var_x;
        return var_y;
    }
}
