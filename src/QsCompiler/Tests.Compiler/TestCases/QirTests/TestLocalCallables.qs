// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR {

    operation DoNothing() : Unit is Adj + Ctl {
        body intrinsic;
    }

    function ReturnTuple (arg : String) : (String, (Int, Double)){
        return (arg, (1, 0.));
    }

    operation Foo(id : Int) : Unit {}
    operation Bar(id : Int) : Unit {}

    operation LazyConstruction(cond : Bool) : (Int => Unit)[] {
        let op = cond ? Foo | Bar;
        op(5);
        (cond ? Foo | Bar)(4);
        let tuple = ((cond ? Foo | Bar), 3);
        let (op2, arg) = tuple;
        op2(arg);
        return [Foo, cond ? Foo | Bar];
    }

    @EntryPoint()
    operation TestLocalCallables () : (String, Double) {

        let arr = [DoNothing];
        Adjoint arr[0]();
        Controlled arr[0]([], ());
        arr[0]();

        let fct = ReturnTuple;
        let (str, (_, val)) = fct("");
        let ops = LazyConstruction(true);
        ops[1](2);
        return (str, val);
    }
}
