namespace Example {
    @EntryPoint()
    operation Main() : Int
    {
        QuantumProgram(3,2,1);
        QuantumProgram(4,9,4);
        return 0;
    }

    function X(value: Int): Int
    {
        return 3 * value;
    }

    operation QuantumProgram(x: Int, h: Int, g: Int) : Unit {
        let z = x * (x + 1) - 47;
        let y = 3 * x;

        use qubits0 = Qubit[9];
        use qubits1 = Qubit[(y - 2)/2-z];
        use qubits2 = Qubit[y - g];
        use qubits3 = Qubit[h];
        use qubits4 = Qubit[X(x)];
    }
}