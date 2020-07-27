namespace Quantum.VizSimulator {
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Measurement;

    operation Foo((alpha: Double, beta: Double), (qubit: Qubit, name: String)): Unit is Ctl {

    }

    operation Test(): Unit {
        using (qs = Qubit[3]) {
            H(qs[0]);
            Ry(2.5, qs[1]);
            Foo((1.0, 2.0), (qs[0], "foo"));
            X(qs[0]);
            CCNOT(qs[0], qs[1], qs[2]);
            Controlled CNOT([qs[0]], (qs[1], qs[2]));
            Controlled Foo([qs[2]], ((1.0, 2.0), (qs[0], "foo")));
            let res = M(qs[0]);
            // ApplyToEach(H, qs);
            ResetAll(qs);
        }
    }
}
