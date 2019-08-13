/// This namespace contains test cases for loop unrolling
namespace Microsoft.Quantum.Testing.Optimization.LoopUnrolling {
    operation Test () : Int {
		mutable r = 0;
        for (i in [0, 2, 4]) {
	        if (i == 2) {
		        set r = i + 1;
	        }
        }
		return r;
    }
}