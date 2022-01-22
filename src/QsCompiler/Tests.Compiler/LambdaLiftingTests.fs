// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.LiftLambdas
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit
open System.Collections.Immutable

type ResolvedTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>

type LambdaLiftingTests() =

    let compilationManager = new CompilationUnitManager(ProjectProperties.Empty, (fun ex -> failwith ex.Message))

    let getTempFile () =
        new Uri(Path.GetFullPath(Path.GetRandomFileName()))

    let getManager uri content =
        CompilationUnitManager.InitializeFileManager(
            uri,
            content,
            compilationManager.PublishDiagnostics,
            compilationManager.LogException
        )

    let ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine("TestCases", fileName) |> File.ReadAllText
        sourceInput.Split([| "===" |], StringSplitOptions.RemoveEmptyEntries)

    let BuildContent content =

        let fileId = getTempFile ()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    let CompileLambdaLiftingTest testNumber =
        let srcChunks = ReadAndChunkSourceFile "LambdaLifting.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = BuildContent <| shared + srcChunks.[testNumber]
        let processedCompilation = LiftLambdaExpressions.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation

        Signatures.SignatureCheck
            [ Signatures.LambdaLiftingNS ]
            Signatures.LambdaLiftingSignatures.[testNumber - 1]
            processedCompilation

        processedCompilation

    let GetBodyFromCallable call =
        call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)

    let GetLinesFromSpecialization specialization =
        let writer = new SyntaxTreeToQsharp()

        specialization
        |> fun x ->
            match x.Implementation with
            | Provided (_, body) -> Some body
            | _ -> None
        |> Option.get
        |> writer.Statements.OnScope
        |> ignore

        writer.SharedState.StatementOutputHandle
        |> Seq.filter (not << String.IsNullOrWhiteSpace)
        |> Seq.toArray

    let GetCallablesWithSuffix compilation ns (suffix: string) =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name = ns)
        |> GlobalCallableResolutions
        |> Seq.filter (fun x -> x.Key.Name.EndsWith suffix)
        |> Seq.map (fun x -> x.Value)

    let GetCallableWithName compilation ns name =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name = ns)
        |> GlobalCallableResolutions
        |> Seq.find (fun x -> x.Key.Name = name)
        |> (fun x -> x.Value)

    let DoesCallSupportFunctors expectedFunctors call =
        let hasAdjoint = expectedFunctors |> Seq.contains QsFunctor.Adjoint
        let hasControlled = expectedFunctors |> Seq.contains QsFunctor.Controlled

        // Checks the characteristics match
        let charMatch =
            lazy
                (match call.Signature.Information.Characteristics.SupportedFunctors with
                 | Value x -> x.SetEquals(expectedFunctors)
                 | Null -> 0 = Seq.length expectedFunctors)

        // Checks that the target specializations are present
        let adjMatch =
            lazy
                (if hasAdjoint then
                     match call.Specializations |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsAdjoint) with
                     | None -> false
                     | Some x ->
                         match x.Implementation with
                         | SpecializationImplementation.Generated gen ->
                             gen = QsGeneratorDirective.Invert || gen = QsGeneratorDirective.SelfInverse
                         | SpecializationImplementation.Provided _ -> true
                         | _ -> false
                 else
                     true)

        let ctlMatch =
            lazy
                (if hasControlled then
                     match call.Specializations |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsControlled) with
                     | None -> false
                     | Some x ->
                         match x.Implementation with
                         | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Distribute
                         | SpecializationImplementation.Provided _ -> true
                         | _ -> false
                 else
                     true)

        charMatch.Value && adjMatch.Value && ctlMatch.Value

    let AssertCallSupportsFunctors expectedFunctors call =
        Assert.True(
            DoesCallSupportFunctors expectedFunctors call,
            sprintf "Callable %O did not support the expected functors" call.FullName
        )

    let AssertLambdaFunctorsByLine result line parentName expectedFunctors =
        let regexMatch = Regex.Match(line, sprintf "_[a-z0-9]{32}_%s" parentName)
        Assert.True(regexMatch.Success, "The original callable did not have the expected content.")

        GetCallableWithName result Signatures.LambdaLiftingNS regexMatch.Value
        |> AssertCallSupportsFunctors expectedFunctors

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Return Value``() =
        let result = CompileLambdaLiftingTest 1

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(lines.[0] = "return 0;", "The generated callable did not have the expected content.")

        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Without Return Value``() =
        let result = CompileLambdaLiftingTest 2

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            0 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Valued Callable``() =
        let result = CompileLambdaLiftingTest 3

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(
            lines.[0] = "return Microsoft.Quantum.Testing.LambdaLifting.Bar();",
            "The generated callable did not have the expected content."
        )

        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Unit Callable``() =
        let result = CompileLambdaLiftingTest 4

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(
            lines.[0] = "Microsoft.Quantum.Testing.LambdaLifting.Bar();",
            "The generated callable did not have the expected content."
        )

        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``Call Valued Callable Recursive``() =
        let result = CompileLambdaLiftingTest 5

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(
            lines.[0] = "return Microsoft.Quantum.Testing.LambdaLifting.Foo();",
            "The generated callable did not have the expected content."
        )

        let lines = original |> GetLinesFromSpecialization

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

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(
            lines.[0] = "Microsoft.Quantum.Testing.LambdaLifting.Foo();",
            "The generated callable did not have the expected content."
        )

        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let expected = sprintf "let lambda = %O(_);" generated.Parent
        Assert.True(lines.[0] = expected, "The generated call expression did not have the correct arguments.")

    [<Fact>]
    [<Trait("Category", "Parameters")>]
    member this.``Use Closure``() =
        let result = CompileLambdaLiftingTest 7

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne
            |> GetBodyFromCallable

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(lines.[0] = "return (x, y, z);", "The generated callable did not have the expected content.")

        let lines = original |> GetLinesFromSpecialization

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
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> Seq.exactlyOne

        Assert.True(generated.Kind = QsCallableKind.Function, "The generated callable was expected to be a function.")

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Type Parameters``() =
        let testNumber = 11
        let srcChunks = ReadAndChunkSourceFile "LambdaLifting.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = BuildContent <| shared + srcChunks.[testNumber]
        let result = LiftLambdaExpressions.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull result

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
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

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

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

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

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

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

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

        let Foo = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable
        let lines = Foo |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." Foo.Parent Foo.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "Foo" []

        let FooAdj = GetCallableWithName result Signatures.LambdaLiftingNS "FooAdj" |> GetBodyFromCallable
        let lines = FooAdj |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdj.Parent FooAdj.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooAdj" [ QsFunctor.Adjoint ]

        let FooCtl = GetCallableWithName result Signatures.LambdaLiftingNS "FooCtl" |> GetBodyFromCallable
        let lines = FooCtl |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooCtl.Parent FooCtl.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooCtl" [ QsFunctor.Controlled ]

        let FooAdjCtl = GetCallableWithName result Signatures.LambdaLiftingNS "FooAdjCtl" |> GetBodyFromCallable
        let lines = FooAdjCtl |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." FooAdjCtl.Parent FooAdjCtl.Kind
        )

        AssertLambdaFunctorsByLine result lines.[0] "FooAdjCtl" [ QsFunctor.Adjoint; QsFunctor.Controlled ]
