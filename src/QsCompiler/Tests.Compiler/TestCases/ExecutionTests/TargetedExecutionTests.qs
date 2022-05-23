namespace Microsoft.Quantum.Testing.ExecutionTests {

    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Canon;

    operation TestTargetPackageHandling() : Unit {
    
        use qs = Qubit[2];
        Ignore(Measure([PauliX, PauliX], qs));
        ResetAll(qs);
    }
}
