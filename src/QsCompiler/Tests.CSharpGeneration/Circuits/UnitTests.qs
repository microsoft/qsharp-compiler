// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.Core {

    @Attribute()
    newtype Attribute = Unit;
}

namespace Microsoft.Quantum.Diagnostics {

    @Attribute()
    newtype Test = String;
}

namespace Microsoft.Quantum.Tests.UnitTests {
    
    open Microsoft.Quantum.Diagnostics;

    @Test("QuantumSimulator")
    @Test("ToffoliSimulator")
    operation UnitTest1 () : Unit {
    }
    
    @Test("SomeNamespace.CustomSimulator")
    operation UnitTest2() : Unit {
	}

}


