// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Test 1: Rename internal operation call references

namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal operation Foo () : Unit {
	}

	operation Bar () : Unit {
		Foo();
	}
}

// =================================
// Test 2: Rename internal function call references

namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal function Foo () : Int {
		return 42;
	}

	function Bar () : Unit {
		let x = Foo();
		let y = 1 + (Foo() - 5);
		let z = (Foo(), 9);
	}
}

// =================================
// Test 3: Rename internal type references

namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal newtype Foo = Unit;

	internal newtype Bar = (Int, Foo);

	internal function Baz (x : Foo) : Foo {
		return Foo();
	}
}

// =================================
// Test 4: Rename internal references across namespaces

namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal newtype Foo = Unit;

	internal function Bar () : Unit {
	}
}

namespace Microsoft.Quantum.Testing.InternalRenaming.Extra {
	open Microsoft.Quantum.Testing.InternalRenaming;

	function Baz () : Unit {
		return Bar();
	}

	internal function Qux (x : Foo) : Foo {
		return x;
	}
}

// =================================
// Test 5: Rename internal qualified references

namespace Microsoft.Quantum.Testing.InternalRenaming {
	internal newtype Foo = Unit;

	internal function Bar () : Unit {
	}
}

namespace Microsoft.Quantum.Testing.InternalRenaming.Extra {
	function Baz () : Unit {
		return Microsoft.Quantum.Testing.InternalRenaming.Bar();
	}

	internal function Qux (x : Microsoft.Quantum.Testing.InternalRenaming.Foo)
		: Microsoft.Quantum.Testing.InternalRenaming.Foo {
		return x;
	}
}

// =================================
// Test 6: Rename internal attribute references

namespace Microsoft.Quantum.Testing.InternalRenaming {
	@Attribute()
	internal newtype Foo = Unit;

	@Foo()
	function Bar () : Unit {
	}
}
