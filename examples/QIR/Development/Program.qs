namespace Microsoft.Quantum.Qir.Development {
    
    function NewArray<'U> () : 'U[] {
        return new 'U[5];
    }

    @EntryPoint()
    operation RunExample() : String {

        let res = NewArray<Qubit[] => Unit is Adj + Ctl>(); // fails
        //let res = NewArray<Qubit>(); // works fine, even though Qubit also doesn't have a default value

        return "Executed successfully!";
    }
}
