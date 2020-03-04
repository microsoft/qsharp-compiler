// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Test 1
namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal operation RenameMe () : Unit {
	}

	operation Foo () : Unit {
		RenameMe();
	}
}

// =================================
