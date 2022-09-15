namespace Microsoft.Quantum.Testing.FirstExampleNs {
    operation Foo(): Unit {
    }
}

namespace Microsoft.Quantum.Testing.SecondExampleNs {
    operation Bar(): Unit {
    }
}

namespace Microsoft.Quantum.Testing.ThirdExampleNs {
    open Microsoft.Quantum.Testing.FirstExampleNs;
    open Microsoft.Quantum.Testing.SecondExampleNs;
    open Microsoft.Quantum.Testing.FirstExampleNs as A;
    open Microsoft.Quantum.Testing.SecondExampleNs as A;

    operation Baz(): Unit {
    }
}
