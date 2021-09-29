namespace ConstArrayReduction {
    open Microsoft.Quantum.Arrays;

    @EntryPoint()
    operation Main(): Int
    {
        mutable arr = [1,2,3,4,5,6,7,8,9,10];
        set arr w/= 7 <- 1337;
        set arr w/= 3 <- arr[7];

        return arr[3];
    }
}