namespace Microsoft.Quantum.Testing {
    open Microsoft.Quantum.Intrinsic;

    internal operation MeasureEach(qs : Qubit[]) : Result[] {

        mutable res = [Zero, size = Length(qs)];
        for i in 0..Length(qs) {
            if M(qs[i]) == One {
                set res w/= i <- One;
            }
        }

        return res;
    }

    internal operation Ignore<'T> (arg : 'T) : Unit {}

    operation Main() : Int {
    
        use qs = Qubit[2];
        Ignore(MeasureEach(qs));
        return 12345;
    }
}
