namespace Microsoft.Quantum.Qir.Emission {
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Measurement;

    @EntryPoint()
    operation Simple() : Unit {
        Message($"{[1,2]}");
    }

    @EntryPoint()
    operation Sum(nums : Int[]) : Int {
        mutable sum = 0;
        for n in nums {
            set sum = sum + n;
        }
        return sum;
    }

    @EntryPoint()
    operation Add(x : Int, y : Int) : Int {
        Message($"x = {x}");
        Message($"y = {y}");
        return x + y;
    }

    @EntryPoint()
    operation Echo(str : String) : String {
        return str;
    }

    @EntryPoint()
    operation EveryOther(list : Int[]) : Int[] {
        return list[0..2...];
    }

    @EntryPoint()
    operation SampleTeleport(input : Bool) : Result {
        // Use two qubits to form the bell pair.
        use bellPair = Qubit[2];
        H(bellPair[0]);
        CNOT(bellPair[0], bellPair[1]);

        // Encode the boolean input into a third qubit.
        use qubit = Qubit();
        if (input) {
            X(qubit);
        }

        // Teleport the input through the bell pair, using
        // branching based on measurement for the corrections.
        CNOT(qubit, bellPair[0]);
        H(qubit);
        if (M(bellPair[0]) == One) {
            X(bellPair[1]);
        }
        if (MResetZ(qubit) == One) {
            Z(bellPair[1]);
        }

        // Return the measurement of the teleported value.
        let mres = MResetZ(bellPair[1]);
        ResetAll(bellPair);
        return mres;
    }

}