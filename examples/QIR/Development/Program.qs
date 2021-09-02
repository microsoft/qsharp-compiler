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

    function Id<'T>(a : 'T) : 'T { return a; }

    @EntryPoint()
    operation RunExample() : String {

        let id = Id<String[]>;
        let _ = id(["hello"]);
        return "Executed successfully!";
    }
}
