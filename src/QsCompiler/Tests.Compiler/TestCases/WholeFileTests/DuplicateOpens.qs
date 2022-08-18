namespace Microsoft.Quantum.Testing.FirstExampleNs {
    operation Foo(): Unit {
    }
}

namespace Microsoft.Quantum.Testing.SecondExampleNs {
    open Microsoft.Quantum.Testing.FirstExampleNs;
    open Microsoft.Quantum.Testing.FirstExampleNs as A;
    open Microsoft.Quantum.Testing.FirstExampleNs;
    open Microsoft.Quantum.Testing.FirstExampleNs as A;
    open Microsoft.Quantum.Testing.FirstExampleNs as B;

    operation Baz(): Unit {
        A.Foo();
        Foo();
        B.Foo();
    }
}
