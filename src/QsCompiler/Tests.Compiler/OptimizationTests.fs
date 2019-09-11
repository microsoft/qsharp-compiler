// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.OptimizationTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.CompilerOptimization
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit


/// Given a string of valid Q# code, outputs the AST and the callables dictionary
let private buildSyntaxTree code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs")
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message)
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.GetSyntaxTree()   // will wait for any current tasks to finish
    FunctorGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree

/// Given a string of valid Q# code, outputs the optimized AST as a string
let private optimize code =
    let mutable tree = buildSyntaxTree code
    tree <- Optimizations.optimize tree
    let x = SyntaxTreeToQs()
    Seq.iter (x.Transform >> ignore) tree
    x.Output

/// Helper function that saves the compiler output as a test case (in the bin directory)
let private createTestCase path =
    let code = Path.Combine(Path.GetFullPath ".", path + "_input.qs") |> File.ReadAllText
    let optimized = optimize code
    (Path.Combine(Path.GetFullPath ".", path + "_output.txt"), optimized) |> File.WriteAllText

/// Asserts that the result of optimizing the _input file matches the result in the _output file
let private assertOptimization path =
    let code = Path.Combine(Path.GetFullPath ".", path + "_input.qs") |> File.ReadAllText
    let expected = Path.Combine(Path.GetFullPath ".", path + "_output.txt") |> File.ReadAllText
    let optimized = optimize code
    // I remove any \r characters to prevent potential OS compatibility issues
    Assert.Equal(expected.Replace("\r", ""), optimized.Replace("\r", ""))


//////////////////////////////// tests //////////////////////////////////


[<Fact>]
let ``arithmetic evaluation`` () =
    // createTestCase "TestCases/Optimizer/Arithmetic"
    assertOptimization "TestCases/Optimizer/Arithmetic"

[<Fact>]
let ``function evaluation`` () =
    // createTestCase "TestCases/Optimizer/FunctionEval"
    assertOptimization "TestCases/Optimizer/FunctionEval"

[<Fact>]
let ``inlining`` () =
    // createTestCase "TestCases/Optimizer/Inlining"
    assertOptimization "TestCases/Optimizer/Inlining"

[<Fact>]
let ``loop unrolling`` () =
    // createTestCase "TestCases/Optimizer/LoopUnrolling"
    assertOptimization "TestCases/Optimizer/LoopUnrolling"

[<Fact>]
let ``miscellaneous`` () =
    // createTestCase "TestCases/Optimizer/Miscellaneous"
    assertOptimization "TestCases/Optimizer/Miscellaneous"

[<Fact>]
let ``no op`` () =
    // createTestCase "TestCases/Optimizer/NoOp"
    assertOptimization "TestCases/Optimizer/NoOp"

[<Fact>]
let ``partial evaluation`` () =
    // createTestCase "TestCases/Optimizer/PartialEval"
    assertOptimization "TestCases/Optimizer/PartialEval"

[<Fact>]
let ``reordering`` () =
    // createTestCase "TestCases/Optimizer/Reordering"
    assertOptimization "TestCases/Optimizer/Reordering"
