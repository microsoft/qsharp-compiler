namespace Quantum.$safeprojectname$ {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;

    
    @Test("QuantumSimulator")
    operation AllocateQubit () : Unit {

        using (q = Qubit()) {
            Assert([PauliZ], [q], Zero, "Newly allocated qubit must be in |0> state.");
        }
        
        Message("Test passed.");
    }
}
