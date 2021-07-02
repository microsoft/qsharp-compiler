namespace QuantumApplication {
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation Run () : Result[][] {
        use control = Qubit();
        use input = Qubit();
        mutable output = [[Zero]];
        
        X(input);
        Controlled X([input], control);
        
        Y(input);
        Controlled Y([input], control);
        
        Z(input);
        Controlled Z([input], control);
        
        H(input);
        
        let result = M(input);

        Reset(input);
        
        Rx(15.0, input);
        Ry(15.0, input);
        Rz(15.0, input);

        S(input);
        Adjoint S(input);

        T(input);
        Adjoint T(input);

        return output;
    }
}