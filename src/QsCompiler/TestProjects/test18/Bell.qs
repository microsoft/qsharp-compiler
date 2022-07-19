open Microsoft.Quantum.Intrinsic;
open Microsoft.Quantum.Canon;

@EntryPoint()
operation Main(): Unit {
    H(q[0]);
    CNOT(q[0], q[1]);

    let result = [M(q[0]), M(q[1])];
    Message($"Results: {result}");
}
