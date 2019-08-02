/// This namespace contains test cases for operation inlining
namespace Microsoft.Quantum.Testing.Optimization.Inlining {

    operation Test (q : Qubit) : Unit {
		f(q, 5);
    }

    function X (q : Qubit) : Unit {
        body intrinsic;
    }

	function f (q : Qubit, x : Int) : Unit {
		if (x == 0) {
			// Do nothing
		}
		elif (x == 1) {
			X(q);
		}
		else {
			f(q, x-1);
			f(q, x-2);
		}
	}
}