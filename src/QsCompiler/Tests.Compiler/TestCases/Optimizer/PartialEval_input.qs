/// This namespace contains test cases for arithmetic optimization
namespace Microsoft.Quantum.Testing.Optimization.PartialEval {
    newtype MyInt = Int;

    operation Test () : Unit {
	    let t = k(_, h(_, 3, _));
	    let u = t(2);
	    let v = ((h(_, 4, _))(5, _))(6);
	    let m = MyInt(5);
	    let n = m!;
	    let o = M(2);
	    let p = o == One;
    }

    function M (q : Int) : Result {
        body intrinsic;
    }

	function h (a : Int, b : Int, c : Int) : Int {
		return a + b + c;
	}

	function k (a : Int, fun : ((Int, Int) -> Int)) : Int {
		return fun(a, a);
	}
}