namespace Microsoft.Quantum.Samples {
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Measurement;
    open Microsoft.Quantum.Intrinsic;

    operation DoStuff(register: Qubit[]): Unit {
        X(register[0]);
        X(register[1]);
        X(register[2]);
        X(register[3]);
        X(register[4]);
        X(register[5]);
        X(register[6]);
        X(register[7]);
        X(register[9]);
        X(register[10]);
        X(register[11]);
        X(register[12]);
        X(register[13]);
        X(register[14]);
        X(register[15]);
        X(register[16]);
        X(register[17]);
        X(register[19]);

         // Uncomment this line and arrays no longer optimized out...
    }

    @EntryPoint()
    operation Test() : Unit {
        use (q0,q1,q2,q3,q4,q5,q6,q7,q8,q9,q10,q11,q12,q13,q14,q15,q16,q17,q18,q19) = (
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
        let arr = [q0,q1,q2,q3,q4,q5,q6,q7,q8,q9,q10,q11,q12,q13,q14,q15,q16,q17,q18,q19];

        DoStuff(arr);
    }
}