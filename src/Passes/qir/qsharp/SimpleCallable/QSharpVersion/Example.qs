namespace Microsoft.Quantum.Samples {
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Intrinsic;
    function Xx(): Int
    {
        return 9;
    }

    function T(q: Int): Bool
    {
        return q > 0;
    }

    @EntryPoint()
    operation CountNumbers() : Int {
        let x  = [2,0,2,0,1,1,1,1,1,0];

        let y = Count<Int>(T, x);
        use (q0,q1,q2,q3,q4,q5,q6,q7,q8,q9) = (
            Qubit(),
            Qubit(),            
            Qubit(),
            Qubit(),            
            Qubit(),
            Qubit(),            
            Qubit(),
            Qubit(),            
            Qubit(),
            Qubit()
            );
        let arr = [q0,q1,q2,q3,q4,q5,q6,q7,q8,q9];

        X(arr[3]);

        return y;
    }
}
