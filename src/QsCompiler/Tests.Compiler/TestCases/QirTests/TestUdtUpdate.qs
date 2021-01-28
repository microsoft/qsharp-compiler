// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Testing.QIR
{
    newtype Complex = (Re : Double, Im : Double);
    newtype TestType = ((Double, A : String), B : Int);
    newtype NamedValue = (Name : String, Value : Complex, Abs : Complex);

    function TestUdtUpdate1(a : String, b : Int) : TestType
    {
        mutable x = TestType((1.0, a), b);
        set x w/= A <- "Hello";
        return x;
    }

    function TestUdtUpdate2(cond : Bool, arg : NamedValue) : NamedValue
    {
        mutable namedValue = arg;
        if (arg::Name == "None")
        {
            set namedValue w/= Value <- Complex(0., 0.);
            if (cond)
            {
                set namedValue w/= Value <- Complex(1., 0.);
            }
            set namedValue w/= Abs <- namedValue::Value;
        }

        return namedValue;
    }
}
