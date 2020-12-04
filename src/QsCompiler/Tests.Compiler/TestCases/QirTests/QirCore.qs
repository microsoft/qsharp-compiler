// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Core {

    // only here to force a certain order for QIR emission (the String argument will do) 
    // to have the tests be a tiny bit less fragile
    internal newtype _Dummy = String;

    @Attribute()
    newtype Attribute = Unit;

    @Attribute()
    newtype Inline = Unit;

    @Attribute()
    newtype EntryPoint = Unit;

    function Length<'T> (array : 'T[]) : Int { body intrinsic; }

    function RangeStart (range : Range) : Int { body intrinsic; }

    function RangeStep (range : Range) : Int { body intrinsic; }

    function RangeEnd (range : Range) : Int { body intrinsic; }

    function RangeReverse (range : Range) : Range { body intrinsic; }
}

namespace Microsoft.Quantum.Targeting {

    @Attribute()
    newtype TargetInstruction = String;
}
