namespace Quantum.QSharpLibrary1 {

    open Microsoft.Quantum.Canon;
    open Microsoft.Quantum.Chemistry;
    open Microsoft.Quantum.Intrinsic;
    open Quantum.ReferenceLibrary;
    

    operation HelloQ () : Unit {
        Message("Hello quantum world!");
        LibraryOperation();
        let x = HTerm([], []);
    }
}
