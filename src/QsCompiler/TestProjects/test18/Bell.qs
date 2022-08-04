// TODO: Uncomment this once references are fixed for notebooks
//open Microsoft.Quantum.Intrinsic;
//open Microsoft.Quantum.Canon;
//@EntryPoint()
operation Main(): Unit {
    AnotherOp();

    // TODO: Uncomment this once references are fixed for notebooks
    //H(q[0]);
    //CNOT(q[0], q[1]);
    //let result = [M(q[0]), M(q[1])];
    //Message($"Results: {result}");
}

operation AnotherOp(): Unit {
    let sum = 1 + 2;
}

newtype Complex = (Real: Double, Imaginary : Double);
