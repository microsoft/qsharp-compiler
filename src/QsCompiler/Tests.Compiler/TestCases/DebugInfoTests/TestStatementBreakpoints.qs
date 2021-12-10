namespace Microsoft.Quantum.Testing.QirDebugInfo {

    @EntryPoint()
    operation Main() : Int {
        mutable boolean = true;

        if (not boolean) {
            return 1;
        }

        set boolean = false;

        if (not boolean) {
            return 0;
        }

        return 1;
    }
}
