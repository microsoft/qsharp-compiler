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

        let coefficients = [
            ComplexPolar(-0.0003573, 3.14159265358979312),
            ComplexPolar(-0.06346, 3.14159265358979312),
            ComplexPolar(1.0, 0.0)];

        mutable ret = coefficients;
        for idxNegative in [0, 1] {

            let coefficient = coefficients[idxNegative];
            set ret w/= idxNegative <- ComplexPolar(coefficient::Magnitude, 0.0);
        }

        return "Executed successfully!";
    }
}
