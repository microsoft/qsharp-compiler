namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Unit {
        let var_x = 42;
        let var_y = IntToInt(var_x);
        let var_z = ToInt();
    }

    operation IntToInt(var_x: Int) : Int {
        return var_x + 1;
    }

    operation ToInt() : Int {
        return 26;
    }
}
