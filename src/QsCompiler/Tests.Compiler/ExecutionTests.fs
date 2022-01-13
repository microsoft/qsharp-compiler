// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open System.Reflection
open System.Text
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Xunit
open Xunit.Abstractions


type ExecutionTests(output: ITestOutputHelper) =

    let WS = new Regex(@"\s+")
    let stripWS str = WS.Replace(str, "")

    let AssertEqual expected got =
        Assert.True(stripWS expected = stripWS got, sprintf "expected: \n%s\ngot: \n%s" expected got)

    let ExecuteOnReferenceTarget engineIdx args =
        let exitCode, ex = ref -101, ref null
        let out, err = ref (new StringBuilder()), ref (new StringBuilder())
        let exe = File.ReadAllLines("ReferenceTargets.txt").[engineIdx]
        let args = sprintf "\"%s\" %s" exe args
        let ranToEnd = ProcessRunner.Run("dotnet", args, out, err, exitCode, ex, timeout = 10000)
        Assert.False(String.IsNullOrWhiteSpace exe)
        Assert.True(ranToEnd)
        Assert.Null(!ex)
        !exitCode, (!out).ToString(), (!err).ToString()

    let ExecuteAndCompareOutput cName expectedOutput =
        let args = sprintf "%s.%s" "Microsoft.Quantum.Testing.ExecutionTests" cName
        let exitCode, out, err = args |> ExecuteOnReferenceTarget 0
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err
        AssertEqual expectedOutput out

    let WriteBitcode pathToBitcode files =
        let pathToBitcode = Path.GetFullPath(pathToBitcode)
        let outputDir = Path.GetDirectoryName(pathToBitcode)
        let projName = Path.GetFileNameWithoutExtension(pathToBitcode)

        let compilerArgs =
            seq {
                yield "build"
                yield "-o"
                yield outputDir
                yield "--proj"
                yield projName
                yield "--build-exe"
                yield "--input"

                for file in files do
                    yield file

                yield "--load"

                yield
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Microsoft.Quantum.QirGeneration.dll"
                    )

                yield "--assembly-properties"
                yield "QirOutputPath:qir"
            }

        let result = Program.Main(compilerArgs |> Seq.toArray)
        Assert.Equal(ReturnCode.Success, result)

    let compiledQirExecutionTest =
        let inputPaths =
            [
                ("TestCases", "QirTests", "ExecutionTests.qs") |> Path.Combine |> Path.GetFullPath
                ("TestCases", "QirTests", "QirCore.qs") |> Path.Combine |> Path.GetFullPath
            ]

        let bitcodePath = ("outputFolder", "ExecutionTests.bc") |> Path.Combine |> Path.GetFullPath
        WriteBitcode bitcodePath inputPaths
        bitcodePath

    let QirExecutionTest functionName =
        output.WriteLine(sprintf "Testing execution of %s:\n" functionName)
        let args = sprintf "%s %s" compiledQirExecutionTest functionName
        let exitCode, out, err = args |> ExecuteOnReferenceTarget 1
        output.WriteLine(out)
        exitCode, out, err


    [<Fact>]
    member this.``QIR entry point return value``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__NoReturn"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err
        AssertEqual "()" out

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__ReturnsUnit"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err
        AssertEqual "()" out

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__ReturnsString"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err
        AssertEqual "\"Success!\"" out // the quotes are correct and needed here


    [<Fact>]
    member this.``QIR string interpolation``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestInterpolatedStrings"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err

        let expected =
            """
            simple string
            "interpolated string"
            true or false, true, false, true, false
            1, -1, 0
            1.0, 2.0, 100000.0, 0.10000000000000001, -1.0, 0.0
            Zero, One
            PauliZ, PauliX, PauliY, [PauliI]
            1..3, 3..-1..1, 0..-1..0
            [1, 2, 3], ["1", "2", "3"], [1, 2, 3], ["1", "2", "3"], ["", "2", "", "4"]
            (), (1, (2, 3)), ("1", ("2", "3")), (1, (2, 3)), ("1", ("2", "3")), ("1", ("", "3"))
            0, [1, 2, 3]
            <function>, <operation>, (<function>, <operation>)
            "Hello", Microsoft.Quantum.Testing.ExecutionTests.Foo(1), Microsoft.Quantum.Testing.ExecutionTests.Tuple("Hello", "World")
            "All good!"
            """

        AssertEqual expected out


    [<Fact>]
    member this.``QIR default values``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestDefaultValues"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err

        let expected =
            """
            [0, 0, 0]
            [0.0, 0.0, 0.0]
            [false, false, false]
            [PauliI, PauliI, PauliI]
            ["", "", ""]
            [0..-1, 0..-1, 0..-1]
            [0, 0, 0]
            [Zero, Zero, Zero]
            [(), (), ()]
            [("", 0), ("", 0), ("", 0)]
            [[], [], []]
            [Microsoft.Quantum.Testing.ExecutionTests.MyUdt(("", ("", Microsoft.Quantum.Testing.ExecutionTests.Tuple("", ""))), ""), Microsoft.Quantum.Testing.ExecutionTests.MyUdt(("", ("", Microsoft.Quantum.Testing.ExecutionTests.Tuple("", ""))), ""), Microsoft.Quantum.Testing.ExecutionTests.MyUdt(("", ("", Microsoft.Quantum.Testing.ExecutionTests.Tuple("", ""))), "")]
            [<function>, <function>, <function>]
            [<operation>, <operation>, <operation>]
            ()
            """

        AssertEqual expected out


    [<Fact>]
    member this.``QIR array slicing``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestArraySlicing"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err

        let expected =
            """
            [1, 2, 3, 4], [4, 3, 2, 1]
            [4, 3, 2, 1], [1, 2, 3, 4]
            [4, 3, 2, 1], [2, 2, 1, 4]
            [4, 3, 2, 1], [2, 2, 1, 4]
            [[1], [0], []], [[3], [2], [1]]
            [[0], [2], []], [[1], [2], [3]]
            [[5], [10], [5], [10], [5]], [[1], [2], [3]]
            [[0], [0, 0], [1, 1, 1]]
            [[0, 0], [0], [1, 1, 1]]
            [[1, 1, 1], [0], [0, 0]]
            [[0, 0], [0], [1, 1, 1]]
            [1, 2, 3, 4], [1, 2, 3, 4]
            1..3..5
            """

        AssertEqual expected out


    [<Fact>]
    member this.``QIR memory management``() =

        // Sanity test to check if we properly detect when a runtime exception is thrown:
        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__CheckFail"
        let exitCode, out, err = QirExecutionTest functionName
        Assert.NotEqual(0, exitCode)
        AssertEqual String.Empty err
        AssertEqual "expected failure in CheckFail" out

        // ... and now the actual tests
        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__RunExample"
        let exitCode, _, err = QirExecutionTest functionName
        Assert.Equal(0, exitCode)
        AssertEqual String.Empty err


    [<Fact>]
    member this.``Loading via test names``() =
        ExecuteAndCompareOutput "LogViaTestName" "not implemented"


    [<Fact>]
    member this.``Specialization Generation for Conjugations``() =
        ExecuteAndCompareOutput
            "ConjugationsInBody"
            "
                U1
                V1
                U3
                V3
                Core3
                Adjoint V3
                Adjoint U3
                Core1
                U2
                V2
                Core2
                Adjoint V2
                Adjoint U2
                U3
                V3
                Adjoint Core3
                Adjoint V3
                Adjoint U3
                Adjoint V1
                Adjoint U1
            "

        ExecuteAndCompareOutput
            "ConjugationsInAdjoint"
            "
                U1
                V1
                U3
                V3
                Core3
                Adjoint V3
                Adjoint U3
                U2
                V2
                Adjoint Core2
                Adjoint V2
                Adjoint U2
                Adjoint Core1
                U3
                V3
                Adjoint Core3
                Adjoint V3
                Adjoint U3
                Adjoint V1
                Adjoint U1
            "

        ExecuteAndCompareOutput
            "ConjugationsInControlled"
            "
                U1
                V1
                U3
                V3
                Core3
                Adjoint V3
                Adjoint U3
                Controlled Core1
                U2
                V2
                Controlled Core2
                Adjoint V2
                Adjoint U2
                U3
                V3
                Adjoint Core3
                Adjoint V3
                Adjoint U3
                Adjoint V1
                Adjoint U1
            "

        ExecuteAndCompareOutput
            "ConjugationsInControlledAdjoint"
            "
                U1
                V1
                U3
                V3
                Core3
                Adjoint V3
                Adjoint U3
                U2
                V2
                Controlled Adjoint Core2
                Adjoint V2
                Adjoint U2
                Controlled Adjoint Core1
                U3
                V3
                Adjoint Core3
                Adjoint V3
                Adjoint U3
                Adjoint V1
                Adjoint U1
            "


    [<Fact>]
    member this.``Referencing Projects and Packages``() =
        ExecuteAndCompareOutput
            "PackageAndProjectReference"
            "
                Welcome to Q#!
                Info: Go check out https://docs.microsoft.com/azure/quantum.
            "

        ExecuteAndCompareOutput
            "TypeInReferencedProject"
            "
                [Complex((1, 0))]
            "

    [<Fact>]
    member this.``Adjoint generation from expressions should be reversed``() =
        ExecuteAndCompareOutput
            "AdjointExpressions"
            "
                1
                2
                3
                4
                5
                6
                7
                8
                9
                10
                11
                skip lift
                12
                13
                14 () ()
                Adjoint 14 () ()
                Adjoint 13
                Adjoint 12
                Adjoint skip lift
                Adjoint 11
                Adjoint 10
                Adjoint 9
                Adjoint 8
                Adjoint 7
                Adjoint 6
                Adjoint 5
                Adjoint 4
                Adjoint 3
                Adjoint 2
                Adjoint 1
            "
