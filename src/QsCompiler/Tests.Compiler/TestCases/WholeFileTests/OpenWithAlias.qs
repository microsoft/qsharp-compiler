namespace Microsoft.Quantum.Testing.FirstExampleNs {
    operation Foo(): Unit {
    }
}

namespace Microsoft.Quantum.Testing.SecondExampleNs {
    open Microsoft.Quantum.Testing.FirstExampleNs;
    open Microsoft.Quantum.Testing.FirstExampleNs as A;

    operation Baz(): Unit {
        A.Foo();
        Foo();
    }
}
