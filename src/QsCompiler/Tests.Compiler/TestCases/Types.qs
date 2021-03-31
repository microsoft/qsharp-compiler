// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This file contains test cases related to user defined types 
// and test cases such that verifications accross different namespaces can be tested


// needs to be available to test attributes
namespace Microsoft.Quantum.Core {

    @ Attribute()
    newtype IntTupleAttribute = (Int, Int);
}

namespace Microsoft.Quantum.Testing.Attributes {

    @ Attribute()
    newtype IntAttribute = Int;

    @ Attribute()
    newtype IntTupleAttribute = (Int, Int);

    @ Attribute()
    newtype StringAttribute = String;

    @ Attribute()
    newtype BigIntArrayAttribute = BigInt[];

    @ Attribute()
    newtype PauliResultAttribute = (Pauli, Result);

    @ Attribute()
    newtype CustomAttribute = Unit;
}

namespace Microsoft.Quantum.Testing.TypeChecking {

    newtype NamedItems1 = (Re : Int, Im : Int);
    newtype NamedItems2 = (Double, (Re : Int, Im : Int));
    newtype NamedItems3 = ((Re : Int, Im : Int), Double);
    newtype NamedItems4 = (Const : Double, (Double, Double));
    newtype NamedItems5 = ((Int, Int), Phase : Int);
    newtype NamedItems6 = (((Int, Int), Phase : Int), (Const : Double, (Double, Double)));
    newtype NamedItems7 = (Name : Int);
    newtype NamedItems8 = (Name : (Int, Int));
    newtype NamedItems9 = Name : Int;
    newtype NamedItems10 = Name : (Int, Int);
    newtype NamedItems11 = (Name : Int, Name : Int);
    newtype NamedItems12 = (Int, (Name : Int, Name : Int));
    newtype NamedItems13 = (Int, (Name : Int, Double), Name : Int);
    newtype NamedItems14 = (Int, (Name : Int, Double), (Double, Name : Int));

    newtype TupleType1 = Int; 
    newtype TupleType2 = (Int, Int); 
    newtype TupleType3 = (); 
    newtype TupleType4 = (Int, (Int,)); 
    newtype TupleType5 = ((Int, Int excess), Int); 
    newtype TupleType6 = ((Int, Int), Int) excess; 

    newtype OpType1 = (Unit => Unit);
    newtype OpType2 = ((Unit => Unit), Int);
    newtype OpType3 = (Int, (Unit => Unit));
    newtype OpType4 = ((Unit => Unit is Adj), Int);
    newtype OpType5 = (Int, (Unit => Unit is Adj));
    newtype OpType6 = (((Unit => Unit) is Adj), Int);
    newtype OpType7 = (Int, ((Unit => Unit) is Adj));
    newtype OpType8 = Unit => Unit;
    newtype OpType9 = (Unit => Unit, Int);
    newtype OpType10 = (Int, Unit => Unit);
    newtype OpType11 = (Unit => Unit is Adj, Int);
    newtype OpType12 = (Int, Unit => Unit is Adj);
    newtype OpType13 = ((Unit => Unit) is Adj, Int);
    newtype OpType14 = (Int, (Unit => Unit) is Adj);
    newtype OpType15 = ((Int, Qubit) => Int);
    newtype OpType16 = (Qubit => Unit : Adjoint);
    newtype OpType17 = (Qubit => Unit : Adjoint, Controlled);
    newtype OpType18 = ((Int, Qubit, (Qubit, Qubit), Result) => Unit : Adjoint, Controlled);

    newtype FctType1 = (Unit -> Unit);
    newtype FctType2 = ((Unit -> Unit), Int);
    newtype FctType3 = (Int, (Unit -> Unit));
    newtype FctType4 = Unit -> Unit;
    newtype FctType5 = (Unit -> Unit, Int);
    newtype FctType6 = (Int, Unit -> Unit);
    newtype FctType7 = (Fct : (Unit -> Unit), Int);
    newtype FctType8 = (Int, Fct : (Unit -> Unit));

    newtype ArrayType1 = Int[];
    newtype ArrayType2 = (Int, Int)[];
    newtype ArrayType3 = (Name : Int, Int)[];
    newtype ArrayType4 = (Name : Int[]);
    newtype ArrayType5 = (Name : (Int, Int)[]);
    newtype ArrayType6 = Name : Int[];
    newtype ArrayType7 = Name : (Int, Int)[];
    newtype ArrayType8 = (Const : Double, (Double, Double)[]);
    newtype ArrayType9 = ((Int, Int)[], Phase : Int);
    newtype ArrayType10 = (((Int, Int)[], Phase : Int[]), (Const : Double[], (Double, Double)[]));
    newtype ArrayType11 = (Unit => Unit)[];
    newtype ArrayType12 = ((Unit => Unit)[], Int);
    newtype ArrayType13 = (Int, (Unit => Unit)[]);
    newtype ArrayType14 = ((Unit => Unit is Adj)[], Int);
    newtype ArrayType15 = (Int, (Unit => Unit is Adj)[]);
    newtype ArrayType16 = (Unit => Unit is Adj[], Int);
    newtype ArrayType17 = (Int, Unit => Unit is Adj[]);
    newtype ArrayType18 = (Fct : (Unit -> Unit)[][], (Int[], Int)[]);
    newtype ArrayType19 = ((Int, Int[])[], Fct : (Unit -> Unit)[][]);
    newtype ArrayType20 = (Fst : (Int, Int)[], Snd : (Double, Double)[]);
}

namespace Microsoft.Quantum.Testing.GlobalVerification {

    open Microsoft.Quantum.Testing.GlobalVerification.N2;
    open Microsoft.Quantum.Testing.GlobalVerification.N3;


    newtype TypeA1 = (Qubit, TypeA2);
    newtype TypeA2 = (Int);
    newtype TypeA3 = (TypeA2, ((TypeA2, TypeA2),Int));

    newtype TypeB1 = (Qubit, TypeB3);
    newtype TypeB2 = (TypeB1, TypeB1);
    newtype TypeB3 = (TypeB2, ((TypeB2, TypeB1), Int));

    newtype TypeC1 = (TypeC3);
    newtype TypeC2 = (TypeC1, TypeC1); 
    newtype TypeC3 = (TypeC2, ((TypeC2, TypeC2), Int));
            
    newtype TypeD1 = (TypeD3 => TypeD2);
    newtype TypeD2 = (Int => Int);
    newtype TypeD3 = (TypeD2, ((TypeD2, TypeD2), Int));

    newtype TypeE1 = (Qubit, TypeE1);
    newtype TypeE2 = (TypeD2, ((TypeD2 => TypeE2), Int));
    newtype TypeE3 = (TypeE3[] => TypeD2);


    newtype ValidType1 = IntType; 
    newtype ValidType2 = (Microsoft.Quantum.Testing.GlobalVerification.N2.CustomType, Microsoft.Quantum.Testing.GlobalVerification.N3.CustomType); 

    newtype InvalidType1 = CustomType; 
    newtype InvalidType2 = Type2Wrapper; 
    newtype InvalidType3 = (Unit, (Unit, N3type3Wrapper[]));
}

namespace Microsoft.Quantum.Testing.GlobalVerification.N2 {

    open Microsoft.Quantum.Testing.GlobalVerification;

    newtype IntType = Int;
    newtype CustomType = Int;
    newtype Type2Wrapper = InvalidType2;
    newtype N3type3Wrapper = (Unit -> Microsoft.Quantum.Testing.GlobalVerification.N3.N3type3);
}

namespace Microsoft.Quantum.Testing.GlobalVerification.N3 {

    newtype UnitType = Unit; 
    newtype IntType = Int;
    newtype CustomType = Int;
    newtype N3type3 = ((Microsoft.Quantum.Testing.GlobalVerification.InvalidType3 -> Unit), Int);
    newtype Register = Microsoft.Quantum.Testing.GlobalVerification.Qubits;
}

namespace Microsoft.Quantum.Testing.GlobalVerification.N4 {

    open Microsoft.Quantum.Testing.GlobalVerification.N2 as N2;
    open Microsoft.Quantum.Testing.GlobalVerification.N3 as N3;

    newtype IntPair = (N2.IntType, N3.IntType);
    function TakesAnyArg<'T> (arg : 'T) : Unit {}
    function Default<'T> () : 'T {
        return (new 'T[1])[0];
    }
}

/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict4 {
    newtype Dummy = Unit;
}

/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict5 {
    newtype Dummy = Unit;
}

/// Namespace used to test conflict with namespace name
namespace Microsoft.Quantum.Testing.GlobalVerification.NamingConflict6 {
    newtype Dummy = Unit;
}
