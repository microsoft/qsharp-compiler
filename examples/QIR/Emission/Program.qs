namespace Microsoft.Quantum.Qir.Emission {
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation RunProgram() : Unit {
        let _ = Foo();
        return ();
        let _ = Foo();
    }

    internal operation Foo() : Bool {
        return true;
    }
}


