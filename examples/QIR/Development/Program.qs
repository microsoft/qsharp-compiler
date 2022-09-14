namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    @EntryPoint()
    operation RunExample(arr : Int[]) : Bool {

        // Add additional code here
        // for experimenting with and debugging QIR generation.

        mutable sum = 0;
        for item in arr {
            set sum += item;
        }

        return sum % 2 == 0;
    }
}


