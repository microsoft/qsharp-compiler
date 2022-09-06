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

    let ws = Regex(@"\s+")
    let stripWS str = ws.Replace(str, "")

    let assertEqual expected got =
        Assert.True(stripWS expected = stripWS got, sprintf "expected: \n%s\ngot: \n%s" expected got)

    let executeOnReferenceTarget engineIdx args =
        let exitCode, ex = ref -101, ref null
        let out, err = ref (StringBuilder()), ref (StringBuilder())
        let exe = File.ReadAllLines("ReferenceTargets.txt").[engineIdx]
        let args = sprintf "\"%s\" %s" exe args
        let ranToEnd = ProcessRunner.Run("dotnet", args, out, err, exitCode, ex, timeout = 10000)
        Assert.False(String.IsNullOrWhiteSpace exe)
        Assert.True(ranToEnd)
        Assert.Null(!ex)
        !exitCode, (!out).ToString(), (!err).ToString()

    let executeAndCompareOutput cName expectedOutput =
        let args = sprintf "simulate Microsoft.Quantum.Testing.ExecutionTests.%s" cName
        let exitCode, out, err = args |> executeOnReferenceTarget 0
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)
        assertEqual expectedOutput out

    let writeBitcode runtimeCapability targetPackageDll pathToBitcode files =
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

                yield "--references"
                yield Path.GetFullPath "Microsoft.Quantum.QSharp.Foundation.dll"
                yield Path.GetFullPath "Microsoft.Quantum.QSharp.Core.dll"

                yield "--load"

                yield
                    Path.Combine(
                        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Microsoft.Quantum.QirGeneration.dll"
                    )

                if not <| String.IsNullOrWhiteSpace runtimeCapability then
                    yield "--runtime"
                    yield runtimeCapability
                    yield "--force-rewrite-step-execution" // to make sure any target specific transformations actually run

                if not <| String.IsNullOrWhiteSpace targetPackageDll then
                    yield "--target-specific-decompositions"
                    yield Path.GetFullPath targetPackageDll

                yield "--assembly-properties"
                yield "QirOutputPath:qir"
                yield $"TargetCapability:{runtimeCapability}"
            }

        let result = Program.Main(compilerArgs |> Seq.toArray)
        Assert.Equal(ReturnCode.Success, result)

    let compiledQirExecutionTest =
        let inputPaths =
            [
                ("TestCases", "ExecutionTests", "QirTests.qs") |> Path.Combine |> Path.GetFullPath
            ]

        let bitcodePath = ("outputFolder", "ExecutionTests.bc") |> Path.Combine |> Path.GetFullPath
        writeBitcode "" "" bitcodePath inputPaths
        bitcodePath

    let compileQirTargetedExecutionTest targetPackageDll =
        let inputPaths =
            [
                if String.IsNullOrWhiteSpace targetPackageDll then
                    yield ("TestCases", "ExecutionTests", "QirDataTypeTests.qs") |> Path.Combine |> Path.GetFullPath
                else
                    yield ("TestCases", "ExecutionTests", "QirTargetingTests.qs") |> Path.Combine |> Path.GetFullPath

            ]

        let bitcodePath = ("outputFolder", "TargetedExecutionTests.bc") |> Path.Combine |> Path.GetFullPath
        writeBitcode "AdaptiveExecution" targetPackageDll bitcodePath inputPaths
        bitcodePath

    let qirExecutionTest targetPackageDll functionName =
        output.WriteLine(sprintf "Testing execution of %s:\n" functionName)

        let bitcodeFile =
            if not (isNull targetPackageDll) then // an empty string permits targeting without pulling in a specific target package
                compileQirTargetedExecutionTest targetPackageDll
            else
                compiledQirExecutionTest

        let args = sprintf "%s %s" bitcodeFile functionName
        let exitCode, out, err = args |> executeOnReferenceTarget 1
        output.WriteLine(out)
        exitCode, out, err


    [<Fact>]
    member this.``QIR entry point return value``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__NoReturn"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)
        assertEqual "()" out

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__ReturnsUnit"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)
        assertEqual "()" out

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__ReturnsString"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)
        assertEqual "\"Success!\"" out // the quotes are correct and needed here


    [<Fact>]
    member this.``QIR string interpolation``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestInterpolatedStrings"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)

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

        assertEqual expected out


    [<Fact>]
    member this.``QIR default values``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestDefaultValues"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)

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

        assertEqual expected out


    [<Fact>]
    member this.``QIR array slicing``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestArraySlicing"
        let exitCode, out, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)

        let expected =
            """
            [3, 3, 3]
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

        assertEqual expected out


    [<Fact>]
    member this.``QIR memory management``() =

        // Sanity test to check if we properly detect when a runtime exception is thrown:
        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__CheckFail"
        let exitCode, out, err = qirExecutionTest null functionName
        Assert.NotEqual(0, exitCode)
        assertEqual "expected failure in CheckFail" out

        // ... and now the actual tests
        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__RunExample"
        let exitCode, _, err = qirExecutionTest null functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)


    [<Fact(Skip = "ToDo: Test fails due to corrupted memory issue.")>]
    member this.``QIR native llvm type handling``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestNativeTypeHandling"
        let exitCode, out, err = qirExecutionTest "" functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)

        let expected =
            """
            [1, 2, 3]
            [6, 6, 6]
            item 0 is 1
            item 1 is 2
            item 2 is 3
            [1, 2, 3, 6, 6, 6]
            [6, 6, 2], [2, 3, 6]
            [1, 2, 3], [4, 2, 3]
            Microsoft.Quantum.Testing.ExecutionTests.MyUnit()
            Microsoft.Quantum.Testing.ExecutionTests.MyTuple(5, 1.0), Microsoft.Quantum.Testing.ExecutionTests.MyTuple(1, 2.0), Microsoft.Quantum.Testing.ExecutionTests.MyTuple(1, 1.0)
            Microsoft.Quantum.Testing.ExecutionTests.MyNestedTuple((1, 1.0), 0.0)
            Microsoft.Quantum.Testing.ExecutionTests.MyNestedTuple((1, 3.0), 0.0)
            1, PauliZ
            2, [1, 2]
            [[2, 1], [], [3], [0]]
            [], [0], 3
            [[PauliX, PauliZ], [], [PauliY], [PauliI]]
            [], [PauliI], PauliY
            [[], [], [3], [0]]
            [[2, 1], [-1, -2, -3], [3], [0]]
            [[], [], [PauliY], [PauliI]]
            [[PauliX, PauliZ], [PauliX, PauliX, PauliX], [PauliY], [PauliI]]
            [[], [], [2], [3]]
            [[0, 1], [3, 3, 3], [2], [3]]
            0, [], 2
            [[[2], [1, 0]], [], [[], [3]], [[1, 2, 3, 4], []]]
            [[[2], [1, 0]], [], [], []]
            """

        assertEqual expected out


    [<Fact(Skip = "This first requires additional support in the QIR runtime, specifically implementing support for all target instructions.")>]
    member this.``QIR target package handling``() =

        let functionName = "Microsoft__Quantum__Testing__ExecutionTests__TestTargetPackageHandling"
        let exitCode, out, err = qirExecutionTest "Microsoft.Quantum.Type3.Core.dll" functionName
        assertEqual String.Empty err
        Assert.Equal(0, exitCode)
        assertEqual "" out


    [<Fact>]
    member this.``Loading via test names``() =
        executeAndCompareOutput "LogViaTestName" "not implemented"


    [<Fact>]
    member this.``Specialization Generation for Conjugations``() =
        executeAndCompareOutput
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

        executeAndCompareOutput
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

        executeAndCompareOutput
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

        executeAndCompareOutput
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
        executeAndCompareOutput
            "PackageAndProjectReference"
            "
                Welcome to Q#!
                Info: Go check out https://docs.microsoft.com/azure/quantum.
            "

        executeAndCompareOutput
            "TypeInReferencedProject"
            "
                [Complex((1, 0))]
            "

    [<Fact>]
    member this.``Adjoint generation from expressions should be reversed``() =
        executeAndCompareOutput
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
