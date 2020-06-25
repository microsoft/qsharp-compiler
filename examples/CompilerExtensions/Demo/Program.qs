namespace Microsoft.Quantum.Demo {
    
    function GetPair (n : Int) : (Int, Int) {
        return (n, n+1);
    }

    @EntryPoint()
    operation SampleProgram () : Int {
        let (a, b) = GetPair(5);
        return a + b;
    }
}
