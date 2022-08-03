open Test19.Imaginary;

operation AbsSquared(c: Complex): Double {
    let prod = Product(c, Conjugate(c));
    return prod::Real;
}
