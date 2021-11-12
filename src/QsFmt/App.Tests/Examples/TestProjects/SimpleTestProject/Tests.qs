namespace Quantum.QSharpTestProject1 {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Chemistry;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Quantum.ReferenceLibrary;

    
    @Test("QuantumSimulator")
    operation AllocateQubit () : Unit {

        use q = Qubit();
        AssertMeasurement([PauliZ], [q], Zero, "Newly allocated qubit must be in |0> state.");

        Message("Test passed.");
        LibraryOperation();
        let x = HTerm([], []);
    }
}
