// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// Tests for capability leveling

namespace Microsoft.Quantum.Core { // or whatever namespace you expect it in - Core seems like the right choice
    
    @Attribute()
    newtype Level = Int;

    @Attribute() 
    newtype Attribute = Unit;
}

namespace Microsoft.Quantum.Testing {
	// This should be level 1 since it's intrinsic.
	operation M (q : Qubit) : Result
	{
		body intrinsic;
	}

	// This should be level 1 since it's intrinsic.
	operation H (q : Qubit) : Unit
	{
		body intrinsic;
	}

	// This should be level 1 since everything it does is safe.
	operation Level1 () : Unit
	{
		using (q = Qubit())
		{
			return M(q);
		}
	}

	// This should be level 2 because it has a branch on a measurement result.
	operation Level2 () : Unit
	{
		using (q = Qubit())
		{
			if (M(q) == One)
			{
				H(q);
			}
		}
	}

	// This should be level 5 because it has classical computation.
	operation Level5 (d : Double) : Double
	{
		return 1.0/d;
	}

	// This should be level 4 because of the attribute.
	@Level(4)
	operation Level4 () : Unit
	{
	
	}
}
