namespace Microsoft.Quantum.Qir.Development {
    open Microsoft.Quantum.Samples.SimpleGrover;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation RunExample() : String {
        // TODO: call whatever Q# code

        let res = SearchForMarkedInput(4);
        Message($"{res}");
        return "Program completed successfully.";
    }
}
