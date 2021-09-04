namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Preparation;
    open Microsoft.Quantum.Samples;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.MachineLearning;

    @EntryPoint()
    operation RunExample() : String {

        let coefficients = [(1.0, 0.0), (1.0, 0.0)];

        mutable ret = coefficients;
        for idxNegative in 0 .. 1 {

            let (mag, _) = coefficients[idxNegative];
            set ret w/= idxNegative <- (mag, 0.0);
        }

        return "Executed successfully!";
    }
}
