namespace Feasibility
{
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation QubitMapping() : Unit {
        use qs = Qubit[3];
        for q in 8..10 {
            X(qs[q - 8]);
        }
    }
}