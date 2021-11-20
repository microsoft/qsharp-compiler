namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Unit {
        let var_x = 42;
        IntToUnit(var_x);
        ToUnit();
    }

    operation IntToUnit(var_x: Int) : Unit {

    }

    operation ToUnit() : Unit {

    }
}
