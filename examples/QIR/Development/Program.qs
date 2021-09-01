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

    newtype Foo = Unit;
    internal function Identity<'T>(input : 'T) : 'T { return input; }

    function MappedByIndex<'T, 'U> (mapper : ((Int, 'T) -> 'U), array : 'T[]) : 'U[] {
        mutable resultArray = new 'U[Length(array)];

        for idxElement in IndexRange(array) {
            set resultArray w/= idxElement <- mapper(idxElement, array[idxElement]);
        }

        return resultArray;
    }

    @EntryPoint()
    operation RunExample() : String {

        // FIXME: fails execution indicating a potential memory leak
        //let model = SequentialModel([], [0.], 0.0);
        //let _ = Enumerated([model]);

        // FIXME: throws an exception in QIR generation
        let item = Foo(); //"hello";
        let _ = MappedByIndex(Identity, [item]);

        // FIXME: fails execution without any info
        //let str = "hello";
        //let _ = MappedByIndex(Identity, [str]);

        return "Executed successfully!";
    }
}
