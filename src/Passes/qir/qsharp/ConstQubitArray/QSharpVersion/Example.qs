namespace ConstArrayReduction {
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation Main(): Int
    {
        let y = 7; //Count<Int>(T, x);
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

        for i in 0..y-1
        {
            X(arr[i]);
        }


        return 0;
    }
}
