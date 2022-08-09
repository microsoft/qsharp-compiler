operation AbsSquared(c: Complex): Double {
    let prod = Product(c, Conjugate(c));
    return prod::Real;
}

operation AbsSquared2(c: Im.Complex): Double {
    let prod = Im.Product(c, Im.Conjugate(c));
    return prod::Real;
}

operation AbsSquared3(c: Test20.Imaginary.Complex): Double {
    let prod = Test20.Imaginary.Product(c, Test20.Imaginary.Conjugate(c));
    return prod::Real;
}
