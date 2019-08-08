/// This namespace contains test cases for arithmetic optimization
namespace Microsoft.Quantum.Testing.Optimization.LoopUnrolling {
    operation Test () : Unit {
        for (i in [0, 2, 4]) {
	        if (i == 2) {
		        let r = i + 1;
	        }
        }
    }
}