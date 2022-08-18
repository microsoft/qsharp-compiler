operation Bell(): (Result, Result) {
    use (q1, q2) = (Qubit(), Qubit());
    H(q1);
    CX(q1, q2);
    return (M(q1), M(q2));
}
