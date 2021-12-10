namespace Microsoft.Quantum.Testing.QirDebugInfo {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation RunExample() : Unit {
        // making these mutable so they're not optimized out
        mutable array = [5, 6, 7];
        // mutable bigInt = 5L; // bigint doesn't seem to be supported in the version of the libraries we're using
        mutable func = Add(4, _);
        mutable op = Subtract(4, _);
        mutable pauli = PauliX;
        use qubit = Qubit();
        mutable result = Measure([pauli], [qubit]);
        mutable range =  IndexRange(array);
        mutable str = "Hello";

        UnitToUnit(); // checking unit type as callable return value since we can't define a variable as a unit
    }

    function Add(x: Int, y: Int) : Int {
        return x + y;
    }

    operation Subtract(x: Int, y: Int) : Int {
        return y - x;
    }

    operation UnitToUnit() : Unit {

    }
}
