// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.OptimizationTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.CompilerOptimization
open Microsoft.Quantum.QsCompiler.Transformations
open Xunit




let private buildSyntaxTree code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs") 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.GetSyntaxTree()   // will wait for any current tasks to finish
    FunctorGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree, compilationUnit.Build().Callables


//////////////////////////////// tests //////////////////////////////////


[<Fact>]
let ``basic walk`` () = 
    let tree, callables = Path.Combine(Path.GetFullPath ".", "TestFiles", "test-00.qs") |> File.ReadAllText |> buildSyntaxTree

    let optimizer = new ConstantPropagator(callables)
    tree |> Seq.map optimizer.Transform |> Seq.toList |> ignore

    let found = optimizer.getFoundConstants
    let expected = ["o"; "op"; "op"; "op"]
    Assert.Equal(expected, found)


[<Fact>]
let ``arithmetic evaluation`` () =
    let code = @"
namespace Microsoft.Quantum.Testing {
    operation Test () : Unit {
	    let x = 5;
	    let y = x + 3;
	    let z = y * 5 % 4;
	    let w = z > 1;
	    let a = w ? 2 | y / 2;
    }
}"  
    let tree, callables = code |> buildSyntaxTree

    let optimizer = new ConstantPropagator(callables)
    tree |> Seq.map optimizer.Transform |> Seq.toList |> ignore

    let found = optimizer.getFoundConstants
    let expected = ["a"; "w"; "x"; "y"; "z"]
    Assert.Equal(expected, found)


[<Fact>]
let ``function evaluation`` () =
    let code = @"
namespace Microsoft.Quantum.Testing {
    operation Test () : Unit {
	    let b = f(1, 8);
	    let (c, d, e) = (g2(3), g2(4), g2(5));
	    let s = mySin(2.0);
    }

	function f (x : Int, w : Int) : Int {
		mutable y = 1;
		mutable z = new Int[5];
		set z = z w/ 0 <- x;
		while (z[0] > 0) {
			set y += w;
			set z = z w/ 0 <- z[0] / 2;
		}
		mutable b = 0;
		for (a in z) {
			set b += a;
		}
		return y + b;
	}

	function g1 (x : Int) : Int {
		if (x == 0) {
			return 0;
		}
		if (x == 1) {
			return 1;
		}
		return g1(x-1) + g1(x-2);
	}

	function g2 (x : Int) : Int {
		return x == 0 ? 0 | (x == 1 ? 1 | g2(x-1) + g2(x-2));
	}

	function mySin (x : Double) : Double {
		let y = ArcSinh(x);
		if (y == 0.0) {
			return 2.0;
		}
		return ArcSinh(y);
	}

	function ArcSinh (x : Double) : Double {
        body intrinsic;
    }
}"
    let tree, callables = code |> buildSyntaxTree

    let optimizer = new ConstantPropagator(callables)
    tree |> Seq.map optimizer.Transform |> Seq.toList |> ignore

    let found = optimizer.getFoundConstants
    let expected = ["b"; "c"; "d"; "e"]
    Assert.Equal(expected, found)


[<Fact>]
let ``other evaluation`` () =
    let code = @"
namespace Microsoft.Quantum.Testing {
    newtype MyInt = Int;

    operation Test () : Unit {

	    let t = k(_, h(_, 3, _));
	    let u = t(2);
	    let v = ((h(_, 4, _))(5, _))(6);

	    let m = MyInt(5);
	    let n = m!;

	    let o = M(x);
	    let p = o == One;

        return p;
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
}"
    let tree, callables = code |> buildSyntaxTree

    let optimizer = new ConstantPropagator(callables)
    tree |> Seq.map optimizer.Transform |> Seq.toList |> ignore

    let found = optimizer.getFoundConstants
    let expected = ["m"; "n"; "t"; "u"; "v"]
    Assert.Equal(expected, found)
