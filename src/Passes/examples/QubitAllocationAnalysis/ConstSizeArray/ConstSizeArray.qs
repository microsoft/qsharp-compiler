namespace Microsoft.Quantum.Tutorial
{
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation TeleportAndReset() : Unit {
        use qs = Qubit[3];
        let x = [qs[1], qs[0], qs[2]];
        X(x[0]);
        X(x[1]);
        X(x[2]);
    }
}