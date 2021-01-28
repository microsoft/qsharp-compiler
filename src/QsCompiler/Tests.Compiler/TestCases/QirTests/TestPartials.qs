// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {
    open Microsoft.Quantum.Intrinsic;

    newtype LittleEndian = Qubit[];

    operation Dummy (qs : Qubit[]) : Unit is Adj + Ctl {
    }

    operation InnerNestedTuple(tuple : (Int, Double), (a1 : String, a2 : Qubit)) : Unit is Adj + Ctl {
    }

    operation TakesNestedTuple(tuple : (Int, Double), a1 : String, a2 : Qubit) : Unit is Adj + Ctl {
    }

    operation ApplyToLittleEndian(bareOp : ((Qubit[]) => Unit is Adj + Ctl), register : LittleEndian)
    : Unit is Adj + Ctl {
        bareOp(register!);
    }

    @EntryPoint()
    operation TestPartials (a : Int, b : Double) : Bool {
        let rotate = Rz(0.25, _);
        let unrotate = Adjoint rotate;

        for (i in 0..100) {
            using (qb = Qubit()) {

                rotate(qb);
                unrotate(qb);
                if (M(qb) != Zero)
                {
                    let tuple1 = (a,b);
                    let tuple2 = ("", qb);
                    let partial1 = InnerNestedTuple(tuple1, _);           
                    let partial2 = InnerNestedTuple(_, tuple2);
                    partial1(tuple2);
                    partial2(tuple1);

                    let partial3 = TakesNestedTuple(tuple1, _, _);           
                    let partial4 = TakesNestedTuple(_, "", qb);
                    partial3(tuple2);
                    partial4(tuple1);
                }

                (ApplyToLittleEndian(Adjoint Dummy, _))(LittleEndian([qb]));
            }
        }

        return true;
    }
}
