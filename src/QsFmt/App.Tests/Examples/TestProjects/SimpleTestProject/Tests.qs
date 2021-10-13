namespace Quantum.QSharpTestProject1 {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;

    
    @Test("QuantumSimulator")
    operation AllocateQubit () : Unit {

        use q = Qubit();
        AssertMeasurement([PauliZ], [q], Zero, "Newly allocated qubit must be in |0> state.");

        Message("Test passed.");
    }
}
