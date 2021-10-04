namespace ConstArrayReduction {
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Intrinsic;
    @EntryPoint()
    operation Main(): Int
    {
        use (q1,q2,q3,q4,q5,q6,q7,q8,q9,q10,theQubit) = (
            Qubit(),
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
        mutable arr = [q1,q2,q3,q4,q5,q6,q7,q8,q9,q10];
        set arr w/= 7 <- theQubit;
        set arr w/= 3 <- arr[7];

        H(arr[3]);
        return 0;
    }
}