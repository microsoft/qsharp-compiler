namespace ConstArrayReduction {
    open Microsoft.Quantum.Arrays;

    operation TupleTest() : (Int, Int)
    {
        return (5,3);
    }

    @EntryPoint()
    operation Main(): Int
    {
        let (x, y) = TupleTest();

        return x*y;
    }
}