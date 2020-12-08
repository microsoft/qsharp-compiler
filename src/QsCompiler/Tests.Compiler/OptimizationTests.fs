// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.OptimizationTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.Experimental
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit


/// Given a string of valid Q# code, outputs the AST and the callables dictionary
let private buildCompilation code =
    let fileId = new Uri(Path.GetFullPath "test-file.qs")

    let compilationUnit =
        new CompilationUnitManager(fun ex -> failwith ex.Message)

    let file =
        CompilationUnitManager.InitializeFileManager(fileId, code)

    // spawns a task that modifies the current compilation
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore

    // will wait for any current tasks to finish
    let mutable compilation = compilationUnit.Build().BuiltCompilation

    CodeGeneration.GenerateFunctorSpecializations(compilation, &compilation) |> ignore

    compilation

/// Given a string of valid Q# code, outputs the optimized AST as a string
let private optimize code =
    let mutable compilation = buildCompilation code
    compilation <- PreEvaluation.All compilation

    String.Join(Environment.NewLine, compilation.Namespaces |> Seq.map SyntaxTreeToQsharp.Default.ToCode)

/// Helper function that saves the compiler output as a test case (in the bin directory)
let private createTestCase path =
    let code =
        Path.Combine(Path.GetFullPath ".", path + "_input.qs") |> File.ReadAllText

    let optimized = optimize code

    (Path.Combine(Path.GetFullPath ".", path + "_output.txt"), optimized) |> File.WriteAllText

/// Asserts that the result of optimizing the _input file matches the result in the _output file
let private assertOptimization path =
    let code =
        Path.Combine(Path.GetFullPath ".", path + "_input.qs") |> File.ReadAllText

    let expected =
        Path.Combine(Path.GetFullPath ".", path + "_output.txt") |> File.ReadAllText

    let optimized = optimize code
    // I remove any \r characters to prevent potential OS compatibility issues
    Assert.Equal(expected.Replace("\r", ""), optimized.Replace("\r", ""))


//////////////////////////////// tests //////////////////////////////////


[<Fact>]
let ``arithmetic evaluation`` () =
    // createTestCase "TestCases/OptimizerTests/Arithmetic"
    assertOptimization "TestCases/OptimizerTests/Arithmetic"

[<Fact>]
let ``function evaluation`` () =
    // createTestCase "TestCases/OptimizerTests/FunctionEval"
    assertOptimization "TestCases/OptimizerTests/FunctionEval"

[<Fact>]
let inlining () =
    // createTestCase "TestCases/OptimizerTests/Inlining"
    assertOptimization "TestCases/OptimizerTests/Inlining"

[<Fact>]
let ``loop unrolling`` () =
    // createTestCase "TestCases/OptimizerTests/LoopUnrolling"
    assertOptimization "TestCases/OptimizerTests/LoopUnrolling"

[<Fact>]
let miscellaneous () =
    // createTestCase "TestCases/OptimizerTests/Miscellaneous"
    assertOptimization "TestCases/OptimizerTests/Miscellaneous"

[<Fact>]
let ``no op`` () =
    // createTestCase "TestCases/OptimizerTests/NoOp"
    assertOptimization "TestCases/OptimizerTests/NoOp"

[<Fact>]
let ``partial evaluation`` () =
    // createTestCase "TestCases/OptimizerTests/PartialEval"
    assertOptimization "TestCases/OptimizerTests/PartialEval"

[<Fact>]
let reordering () =
    // createTestCase "TestCases/OptimizerTests/Reordering"
    assertOptimization "TestCases/OptimizerTests/Reordering"

[<Fact>]
let ``trigger infinite loop`` () =
    // createTestCase "TestCases/OptimizerTests/TypedParameters"
    assertOptimization "TestCases/OptimizerTests/TypedParameters"
