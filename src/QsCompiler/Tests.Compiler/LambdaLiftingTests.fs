// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Immutable
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.LiftLambdas
open Xunit

type ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>

type LambdaLiftingTests() =

    let compileLambdaLiftingTest testNumber =
        let srcChunks = TestUtils.readAndChunkSourceFile "LambdaLifting.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = TestUtils.buildContent <| shared + srcChunks.[testNumber]
        let processedCompilation = LiftLambdaExpressions.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation

        Signatures.SignatureCheck
            [ Signatures.LambdaLiftingNS ]
            Signatures.LambdaLiftingSignatures.[testNumber - 1]
            processedCompilation

        processedCompilation

    let assertLambdaFunctorsByLine result line parentName expectedFunctors =
        let regexMatch = Regex.Match(line, sprintf "__[a-z0-9]{32}__%s" parentName)
        Assert.True(regexMatch.Success, "The original callable did not have the expected content.")

        TestUtils.getCallableWithName result Signatures.LambdaLiftingNS regexMatch.Value
        |> TestUtils.assertCallSupportsFunctors expectedFunctors

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Return Value``() =
        let result = compileLambdaLiftingTest 1

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return 0;" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Without Return Value``() =
        let result = compileLambdaLiftingTest 2

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            0 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Valued Callable``() =
        let result = compileLambdaLiftingTest 3

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return Microsoft.Quantum.Testing.LambdaLifting.Bar();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Unit Callable``() =
        let result = compileLambdaLiftingTest 4

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return Microsoft.Quantum.Testing.LambdaLifting.Bar();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Valued Callable Recursive``() =
        let result = compileLambdaLiftingTest 5

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return Microsoft.Quantum.Testing.LambdaLifting.Foo();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Unit Callable Recursive``() =
        let result = compileLambdaLiftingTest 6

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return Microsoft.Quantum.Testing.LambdaLifting.Foo();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Closure``() =
        let result = compileLambdaLiftingTest 7

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return (x, y, z);" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            5 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(x, y, z, _);" generated.Parent
        Assert.True(lines.[4] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``With Lots of Params``() =
        let result = compileLambdaLiftingTest 8

        let generated =
            [
                ("(a1 : Int)", "Unit")
                ("(a2 : Int)", "Unit")
                ("(a3 : Int, b3 : Double)", "Unit")
                ("(a4 : Int, b4 : Double, c4 : String)", "Unit")
                ("(a5 : Int, (b5 : Double, c5 : String))", "Unit")
                ("(a6 : Int, b6 : Double, c6 : String)", "Unit")
                ("((a7 : Int, b7 : Double), c7 : String)", "Unit")
                ("(a8 : Int, b8 : Double, c8 : String)", "Unit")
            ]
            |> TestUtils.identifyCallablesBySignature (
                TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            )

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        Assert.True(
            8 = (original |> TestUtils.getLinesFromSpecialization |> Seq.length),
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let success =
            [
                (0, sprintf "let lambda1 = (%O(_))(0);" (Seq.item 0 generated).FullName)
                (1, sprintf "let lambda2 = (%O(_))(0);" (Seq.item 1 generated).FullName)
                (2, sprintf "let lambda3 = (%O(_, _))(0, 0.0);" (Seq.item 2 generated).FullName)
                (3, sprintf "let lambda4 = (%O(_, _, _))(0, 0.0, \"Zero\");" (Seq.item 3 generated).FullName)
                (4, sprintf "let lambda5 = (%O(_, _))(0, (0.0, \"Zero\"));" (Seq.item 4 generated).FullName)
                (5, sprintf "let lambda6 = (%O(_, _, _))(0, 0.0, \"Zero\");" (Seq.item 5 generated).FullName)
                (6, sprintf "let lambda7 = (%O(_, _))((0, 0.0), \"Zero\");" (Seq.item 6 generated).FullName)
                (7, sprintf "let lambda8 = (%O(_, _, _))(0, 0.0, \"Zero\");" (Seq.item 7 generated).FullName)
            ]
            |> TestUtils.checkIfSpecializationHasContent original

        Assert.True(success, "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Closure With Params``() =
        let result = compileLambdaLiftingTest 9

        let returnType = "(Double, String, Result)"
        let closureParams = "x : Double, y : String, z : Result"

        let generated =
            [
                "a1 : Int"
                "a2 : Int"
                "a3 : Int, b3 : Double"
                "a4 : Int, b4 : Double, c4 : String"
                "a5 : Int, (b5 : Double, c5 : String)"
                "a6 : Int, b6 : Double, c6 : String"
                "(a7 : Int, b7 : Double), c7 : String"
                "a8 : Int, b8 : Double, c8 : String"
            ]
            |> Seq.map (fun s -> sprintf "(%s, %s)" closureParams s, returnType)
            |> TestUtils.identifyCallablesBySignature (
                TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            )

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        Assert.True(
            12 = (original |> TestUtils.getLinesFromSpecialization |> Seq.length),
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let closureArgs = "x, y, z"

        let success =
            [
                (4, sprintf "let lambda1 = (%O(%s, _))(0);" (Seq.item 0 generated).FullName closureArgs)
                (5, sprintf "let lambda2 = (%O(%s, _))(0);" (Seq.item 1 generated).FullName closureArgs)
                (6, sprintf "let lambda3 = (%O(%s, _, _))(0, 0.0);" (Seq.item 2 generated).FullName closureArgs)
                (7,
                 sprintf
                     "let lambda4 = (%O(%s, _, _, _))(0, 0.0, \"Zero\");"
                     (Seq.item 3 generated).FullName
                     closureArgs)
                (8,
                 sprintf "let lambda5 = (%O(%s, _, _))(0, (0.0, \"Zero\"));" (Seq.item 4 generated).FullName closureArgs)
                (9,
                 sprintf
                     "let lambda6 = (%O(%s, _, _, _))(0, 0.0, \"Zero\");"
                     (Seq.item 5 generated).FullName
                     closureArgs)
                (10,
                 sprintf "let lambda7 = (%O(%s, _, _))((0, 0.0), \"Zero\");" (Seq.item 6 generated).FullName closureArgs)
                (11,
                 sprintf
                     "let lambda8 = (%O(%s, _, _, _))(0, 0.0, \"Zero\");"
                     (Seq.item 7 generated).FullName
                     closureArgs)
            ]
            |> TestUtils.checkIfSpecializationHasContent original

        Assert.True(success, "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Function Lambda``() =
        let result = compileLambdaLiftingTest 10

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo" |> Seq.exactlyOne

        Assert.True(generated.Kind = QsCallableKind.Function, "The generated callable was expected to be a function.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Type Parameters``() =
        let testNumber = 11
        let srcChunks = TestUtils.readAndChunkSourceFile "LambdaLifting.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = TestUtils.buildContent <| shared + srcChunks.[testNumber]
        let result = LiftLambdaExpressions.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull result

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo" |> Seq.exactlyOne

        let originalExpectedName = { Namespace = Signatures.LambdaLiftingNS; Name = "Foo" }
        let ``Foo.A`` = QsTypeParameter.New(originalExpectedName, "A") |> TypeParameter |> ResolvedType.New
        let ``Foo.B`` = QsTypeParameter.New(originalExpectedName, "B") |> TypeParameter |> ResolvedType.New
        let ``Foo.C`` = QsTypeParameter.New(originalExpectedName, "C") |> TypeParameter |> ResolvedType.New

        let originalExpectedArgType =
            [| ``Foo.A``; ``Foo.B``; ``Foo.C`` |]
            |> ImmutableArray.ToImmutableArray
            |> QsTypeKind.TupleType
            |> ResolvedType.New

        let originalExpectedReturnType = ResolvedType.New(ResolvedTypeKind.UnitType)

        let originalSigExpected = originalExpectedName, originalExpectedArgType, originalExpectedReturnType

        let generatedExpectedName = { Namespace = Signatures.LambdaLiftingNS; Name = generated.FullName.Name }
        let ``_Foo.A`` = QsTypeParameter.New(generatedExpectedName, "A") |> TypeParameter |> ResolvedType.New
        let ``_Foo.C`` = QsTypeParameter.New(generatedExpectedName, "C") |> TypeParameter |> ResolvedType.New

        let generatedExpectedArgType =
            [| ``_Foo.A``; ``_Foo.C``; ResolvedType.New(ResolvedTypeKind.UnitType) |]
            |> ImmutableArray.ToImmutableArray
            |> QsTypeKind.TupleType
            |> ResolvedType.New

        let generatedExpectedReturnType =
            [| ``_Foo.C``; ``_Foo.A`` |]
            |> ImmutableArray.ToImmutableArray
            |> QsTypeKind.TupleType
            |> ResolvedType.New

        let generatedSigExpected = generatedExpectedName, generatedExpectedArgType, generatedExpectedReturnType

        Signatures.SignatureCheck
            [ Signatures.LambdaLiftingNS ]
            (seq {
                originalSigExpected
                generatedSigExpected
            })
            result

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Nested Lambda Call``() = compileLambdaLiftingTest 12

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Nested Lambda``() = compileLambdaLiftingTest 13

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Basic Return``() =
        let result = compileLambdaLiftingTest 14

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "Foo" []
        assertLambdaFunctorsByLine result lines.[1] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Call``() =
        let result = compileLambdaLiftingTest 15

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            5 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "Foo" []
        assertLambdaFunctorsByLine result lines.[1] "Foo" []
        assertLambdaFunctorsByLine result lines.[2] "Foo" [ QsFunctor.Adjoint ]
        assertLambdaFunctorsByLine result lines.[3] "Foo" [ QsFunctor.Controlled ]
        assertLambdaFunctorsByLine result lines.[4] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Lambda Call``() =
        let result = compileLambdaLiftingTest 16

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            4 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "Foo" []
        assertLambdaFunctorsByLine result lines.[1] "Foo" [ QsFunctor.Adjoint ]
        assertLambdaFunctorsByLine result lines.[2] "Foo" [ QsFunctor.Controlled ]
        assertLambdaFunctorsByLine result lines.[3] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Recursive``() =
        let result = compileLambdaLiftingTest 17

        let Foo =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = Foo |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." Foo.Parent Foo.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "Foo" []

        let FooAdj =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooAdj"
            |> TestUtils.getBodyFromCallable

        let lines = FooAdj |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdj.Parent FooAdj.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "FooAdj" [ QsFunctor.Adjoint ]

        let FooCtl =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooCtl"
            |> TestUtils.getBodyFromCallable

        let lines = FooCtl |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooCtl.Parent FooCtl.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "FooCtl" [ QsFunctor.Controlled ]

        let FooAdjCtl =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooAdjCtl"
            |> TestUtils.getBodyFromCallable

        let lines = FooAdjCtl |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdjCtl.Parent FooAdjCtl.Kind
        )

        assertLambdaFunctorsByLine result lines.[0] "FooAdjCtl" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``With Missing Params``() = compileLambdaLiftingTest 18

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Use Parameter Single``() =
        let result = compileLambdaLiftingTest 19

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return x;" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent; "let result = lambda(0);" |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Use Parameter Tuple``() =
        let result = compileLambdaLiftingTest 20

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return (y, x);" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent =
            [|
                sprintf "let lambda = %O(_, _);" generated.Parent
                "let result = lambda(0.0, 0);"
            |]

        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Use Parameter and Closure``() =
        let result = compileLambdaLiftingTest 21

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return (a, x);" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent =
            [|
                "let a = 0;"
                sprintf "let lambda = %O(a, _);" generated.Parent
                "let result = lambda(0.0);"
            |]

        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Use Parameter with Missing Params``() =
        let result = compileLambdaLiftingTest 22

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return x;" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent =
            [|
                sprintf "let lambda = %O(_, _, _);" generated.Parent
                "let result = lambda(0, Zero, \"Zero\");"
            |]

        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Multiple Lambdas in One Expression``() =
        let result = compileLambdaLiftingTest 23

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> (fun x ->
                Assert.True(2 = Seq.length x)
                x |> Seq.map TestUtils.getBodyFromCallable)

        let hasFirstContent spec =
            let lines = spec |> TestUtils.getLinesFromSpecialization
            lines = [| "return x + 1;" |]

        let hasSecondContent spec =
            let lines = spec |> TestUtils.getLinesFromSpecialization
            lines = [| "return x + 2;" |]

        let first, second =
            let temp1 = Seq.item 0 generated
            let temp2 = Seq.item 1 generated

            if (hasFirstContent temp1) then
                Assert.True(
                    hasSecondContent temp2,
                    sprintf "Callable %O(%A) did not have expected content" temp2.Parent QsSpecializationKind.QsBody
                )

                temp1, temp2
            else
                Assert.True(
                    hasFirstContent temp2,
                    sprintf "Callable %O(%A) did not have expected content" temp2.Parent QsSpecializationKind.QsBody
                )

                Assert.True(
                    hasSecondContent temp1,
                    sprintf "Callable %O(%A) did not have expected content" temp1.Parent QsSpecializationKind.QsBody
                )

                temp2, temp1

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambdaTuple = (%O(_), %O(_));" first.Parent second.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Function Without Return Value``() =
        let result = compileLambdaLiftingTest 24

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            0 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Return Unit-Typed Expression``() =
        let result = compileLambdaLiftingTest 25

        let original =
            TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "return x;" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = (%O(_))();" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")
