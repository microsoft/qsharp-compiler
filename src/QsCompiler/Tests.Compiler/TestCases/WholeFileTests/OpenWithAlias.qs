namespace Microsoft.Quantum.Testing.FirstExampleNs {
    operation Foo(): Unit {
    }

    newtype Complex = (Real: Double, Imaginary : Double);
}

namespace Microsoft.Quantum.Testing.SecondExampleNs {
    open Microsoft.Quantum.Testing.FirstExampleNs;
    open Microsoft.Quantum.Testing.FirstExampleNs as A;

    operation Baz(): Unit {
        A.Foo();
        Foo();

        let i1 = Complex(0.0, 1.0);
        let i2 = A.Complex(0.0, 1.0);
    }
}
