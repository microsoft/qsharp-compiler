operation Main(): Unit {
    AnotherOp();
}

operation AnotherOp(): Unit {
    let sum = 1 + 2;
}

newtype Complex = (Real: Double, Imaginary : Double);
