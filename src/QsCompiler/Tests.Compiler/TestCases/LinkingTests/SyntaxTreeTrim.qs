// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

// Trimmer Removes Unused Callables
namespace Microsoft.Quantum.Testing.SyntaxTreeTrimming {
    
    @EntryPoint()
    operation Main() : Unit {
        UsedOp();
    }

    operation UsedOp() : Unit { UsedFunc(); }
    operation UnusedOp() : Unit { UnusedFunc(); }

    function UsedFunc() : Unit { }
    function UnusedFunc() : Unit { }
}

// =================================

// Trimmer Keeps UDTs
namespace Microsoft.Quantum.Testing.SyntaxTreeTrimming {

    newtype UsedUDT = Int;
    newtype UnusedUDT = String;

    @EntryPoint()
    operation Main() : Unit {
        let x = UsedUDT(3);
    }
}

// =================================

// Trimmer Keeps Intrinsics When Told
namespace Microsoft.Quantum.Testing.SyntaxTreeTrimming {

    @EntryPoint()
    operation Main() : Unit {
        UsedIntrinsic();
    }

    operation UsedIntrinsic() : Unit { body intrinsic; }
    operation UnusedIntrinsic() : Unit { body intrinsic; }
}

// =================================

// Trimmer Removes Intrinsics When Told
namespace Microsoft.Quantum.Testing.SyntaxTreeTrimming {

    @EntryPoint()
    operation Main() : Unit {
        UsedIntrinsic();
    }

    operation UsedIntrinsic() : Unit { body intrinsic; }
    operation UnusedIntrinsic() : Unit { body intrinsic; }
}
