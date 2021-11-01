// purposefully leaving space for testing

namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Core;

    newtype ContinuousOracle = (((Double, Qubit[]) => Unit is Adj + Ctl));

    @EntryPoint()
    operation RunExample() : Int {
        mutable var_y = InternalFunc();
        return var_y;
    }

    operation InternalFunc() : Int {
        mutable var_x = 26;
        return var_x;
    }
}
