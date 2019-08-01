/// This namespace contains test cases for operation inlining
namespace Microsoft.Quantum.Testing.Optimization.Inlining {

    operation Test () : Unit {
		f(5);
    }

    function X (q : Int) : Unit {
        body intrinsic;
    }

	function f (q: Qubit, x : Int) : Unit {
		if (x == 0) {
			// Do nothing
		}
		elif (x == 1) {
			X(q);
		}
		else {
			f(x-1);
			f(x-2);
		}
	}
}