/// This namespace contains test cases for arithmetic optimization
namespace Microsoft.Quantum.Testing.Optimization.Arithmetic {
    operation Test () : Unit {
	    let x = 5;
	    let y = x + 3;
	    let z = y * 5 % 4;
	    let w = z > 1;
	    let a = w ? 2 | y / 2;
    }
}