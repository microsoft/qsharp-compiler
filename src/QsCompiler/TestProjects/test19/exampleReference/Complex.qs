namespace Test19.Imaginary {
    newtype Complex = (Real: Double, Imaginary : Double);

    function Conjugate(c: Complex): Complex {
        return Complex(c::Real, -c::Imaginary);
    }

    operation Product(c1: Complex, c2: Complex): Complex {
        return Complex(c1::Real*c2::Real - c1::Imaginary*c2::Imaginary, c1::Real*c2::Imaginary + c1::Imaginary*c2::Real);
    }
}
