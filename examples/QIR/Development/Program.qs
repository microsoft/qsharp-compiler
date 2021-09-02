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

    function Mapped<'T, 'U> (mapper : ('T -> 'U), array : 'T[]) : 'U[] {
        mutable resultArray = new 'U[Length(array)];

        for idxElement in IndexRange(array) {
            set resultArray w/= idxElement <- mapper(array[idxElement]);
        }

        return resultArray;
    }

    newtype SequentialModel = Double[];

    @EntryPoint()
    operation RunExample() : String {

        let _ = Mapped(SequentialModel, [[0.]]);
        return "Executed successfully!";
    }
}
