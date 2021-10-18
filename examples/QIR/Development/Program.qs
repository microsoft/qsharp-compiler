namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    @EntryPoint()
    operation RunExample() : Int {

        mutable var_x = 26;
        return var_x;

    }
}


