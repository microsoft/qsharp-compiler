namespace Example {
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Canon;
        
    @EntryPoint()
    operation Main() : Int
    {
        use qubits2 = Qubit[3];                
        use qubits1 = Qubit[3];        

        X(qubits1[0]);
        X(qubits1[1]);
        X(qubits1[2]);

        X(qubits2[0]);
        X(qubits2[1]);
        X(qubits2[2]);

        return 0;
    }
}
