// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Test 1
namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal operation Foo () : Unit {
	}

	operation Bar () : Unit {
		Foo();
	}
}

// =================================

// Test 2
namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal newtype Foo = Unit;

	internal function Bar (x : Foo) : Unit {
	}
}
