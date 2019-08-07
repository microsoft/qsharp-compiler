/// This namespace contains test cases for arithmetic optimization
namespace Microsoft.Quantum.Testing.Optimization.PartialEval {
    newtype MyInt = Int;
	newtype MyFunc = (Int -> Int);

    operation Test () : (Int, Int, Int, Int, Int, Int, Bool) {
	    let t = k(_, h(_, 3, _));
	    let u = t(2);
	    let v = ((h(_, 4, _))(5, _))(6);

		let x = j(_, (0, _, _));
		let y = x(1, (2, 3));
		let z = (x(_, (0, _)))(1, 2);

		let a = MyFunc(x(5, (_, 5)));
		let b = (a!)(_);
		let c = b(2);

	    let m = MyInt(5);
	    let n = m!;

	    let o = M(2);
	    let p = o == One;

		return (u, v, y, z, c, n, p);
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

	function j (a : Int, (b : Int, c : Int, d : Int)) : Int {
		return a + b + c + d;
	}
}