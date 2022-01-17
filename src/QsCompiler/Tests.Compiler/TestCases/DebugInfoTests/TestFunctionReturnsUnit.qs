namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Unit {
        let varX = 42;
        IntToUnit(varX);
        ToUnit();
    }

    operation IntToUnit(varX: Int) : Unit {

    }

    operation ToUnit() : Unit {

    }
}
