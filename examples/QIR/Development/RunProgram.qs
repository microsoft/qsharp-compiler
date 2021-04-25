namespace Microsoft.Quantum.Qir.Development {
    open Microsoft.Quantum.Samples.RepeatUntilSuccess;
    open Microsoft.Quantum.Samples.OrderFinding;
    open Microsoft.Quantum.Samples.SimpleGrover;
    open Microsoft.Quantum.Intrinsic;

    @EntryPoint()
    operation RunExample() : String {

        let res = SearchForMarkedInput(4);
        Message($"Grover: {res}");
        let order = GetOrder(2);

        for n in 0 .. 3 {

            // TODO: requires asserts

            //mutable (success, result, numIter) = CreateQubitsAndApplySimpleGate(true, PauliX, 100);
            //Message($"Simple rus: ({success}, {result}, {numIter})");

            //set (success, result, numIter) = CreateQubitsAndApplyRzArcTan2(true, PauliX, 100);
            //Message($"V-gate rus: ({success}, {result}, {numIter})");
        }

        return "Program completed successfully.";
    }
}
