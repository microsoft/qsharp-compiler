namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;

    function UpdateArr(arr : Int[]) : Int[] {
        return arr w/ 0 <- 5;
    }

    @EntryPoint()
    operation RunExample() : Bool {

        // Add additional code here
        // for experimenting with and debugging QIR generation.
        //let local_arr = [1,2,3];
        //mutable sum1 = 0;
        //for idx in local_arr {
        //    set sum1 += local_arr[idx];
        //}

        let arr = [1,2,3];

        mutable sum2 = 0;
        //let new_arr2 = UpdateArr(arr);
        for item in arr {
            set sum2 += item;
        }

        return sum2 % 2 == 0;
    }
}


