namespace Quantum.Test {
    @EntryPoint()
    operation HelloQ () : Result {
        mutable a = 0;
        if (One == One) {
            set a = 1;
        }
        return Zero;
    }
}
