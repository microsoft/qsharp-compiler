namespace Microsoft.Quantum.Qir.Development {

    open Microsoft.Quantum.Characterization;
    open Microsoft.Quantum.Logical;
    open Microsoft.Quantum.Samples;
    open Microsoft.Quantum.Convert;
    open Microsoft.Quantum.Arrays;
    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Diagnostics;
    open Microsoft.Quantum.Intrinsic;
    open Microsoft.Quantum.Math;
    open Microsoft.Quantum.Arithmetic;
    open Microsoft.Quantum.MachineLearning;

    @EntryPoint()
    operation RunExample() : String {

        let encode = _EncodeSample(0.1, 5, _);
        let encodedSamples = Mapped(encode, []); // commenting out this works fine...
        return "Executed successfully!";
    }
}
