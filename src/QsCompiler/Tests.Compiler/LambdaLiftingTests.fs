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

    let CompileLambdaLiftingTest testNumber =
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

    let AssertLambdaFunctorsByLine result line parentName expectedFunctors =
        let regexMatch = Regex.Match(line, sprintf "_[a-z0-9]{32}_%s" parentName)
        Assert.True(regexMatch.Success, "The original callable did not have the expected content.")

        TestUtils.getCallableWithName result Signatures.LambdaLiftingNS regexMatch.Value
        |> TestUtils.assertCallSupportsFunctors expectedFunctors

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Return Value``() =
        let result = CompileLambdaLiftingTest 1

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

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
        let result = CompileLambdaLiftingTest 2

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

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
        let result = CompileLambdaLiftingTest 3

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

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
        let result = CompileLambdaLiftingTest 4

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "Microsoft.Quantum.Testing.LambdaLifting.Bar();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Valued Callable Recursive``() =
        let result = CompileLambdaLiftingTest 5

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

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
        let result = CompileLambdaLiftingTest 6

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = generated |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| "Microsoft.Quantum.Testing.LambdaLifting.Foo();" |]
        Assert.True((lines = expectedContent), "The generated callable did not have the expected content.")

        let lines = original |> TestUtils.getLinesFromSpecialization

        let expectedContent = [| sprintf "let lambda = %O(_);" generated.Parent |]
        Assert.True((lines = expectedContent), "The original callable did not have the expected content.")

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Closure``() =
        let result = CompileLambdaLiftingTest 7

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable

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
    member this.``Use Lots of Params``() = CompileLambdaLiftingTest 8 |> ignore

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Closure With Params``() = CompileLambdaLiftingTest 9 |> ignore

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Function Lambda``() =
        let result = CompileLambdaLiftingTest 10

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne

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
            TestUtils.getCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne

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
    member this.``With Nested Lambda Call``() = CompileLambdaLiftingTest 12

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Nested Lambda``() = CompileLambdaLiftingTest 13

    [<Fact(Skip = "Known Bug: https://github.com/microsoft/qsharp-compiler/issues/1113")>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Basic Return``() =
        let result = CompileLambdaLiftingTest 14

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable
        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "Foo" []
        AssertLambdaFunctorsByLine result lines.[1] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Call``() =
        let result = CompileLambdaLiftingTest 15

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable
        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            5 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "Foo" []
        AssertLambdaFunctorsByLine result lines.[1] "Foo" []
        AssertLambdaFunctorsByLine result lines.[2] "Foo" [ QsFunctor.Adjoint ]
        AssertLambdaFunctorsByLine result lines.[3] "Foo" [ QsFunctor.Controlled ]
        AssertLambdaFunctorsByLine result lines.[4] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact(Skip = "Known Bug: https://github.com/microsoft/qsharp-compiler/issues/1113")>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Lambda Call``() =
        let result = CompileLambdaLiftingTest 16

        let original = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable
        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            4 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "Foo" [] // This line fails due to known bug. See skip reason.
        AssertLambdaFunctorsByLine result lines.[1] "Foo" [ QsFunctor.Adjoint ]
        AssertLambdaFunctorsByLine result lines.[2] "Foo" [ QsFunctor.Controlled ]
        AssertLambdaFunctorsByLine result lines.[3] "Foo" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Functor Support Recursive``() =
        let result = CompileLambdaLiftingTest 17

        let Foo = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "Foo" |> TestUtils.getBodyFromCallable
        let lines = Foo |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." Foo.Parent Foo.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "Foo" []

        let FooAdj = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooAdj" |> TestUtils.getBodyFromCallable
        let lines = FooAdj |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdj.Parent FooAdj.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooAdj" [ QsFunctor.Adjoint ]

        let FooCtl = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooCtl" |> TestUtils.getBodyFromCallable
        let lines = FooCtl |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooCtl.Parent FooCtl.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooCtl" [ QsFunctor.Controlled ]

        let FooAdjCtl = TestUtils.getCallableWithName result Signatures.LambdaLiftingNS "FooAdjCtl" |> TestUtils.getBodyFromCallable
        let lines = FooAdjCtl |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdjCtl.Parent FooAdjCtl.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooAdjCtl" [ QsFunctor.Adjoint; QsFunctor.Controlled ]

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Missing Params``() = CompileLambdaLiftingTest 18
