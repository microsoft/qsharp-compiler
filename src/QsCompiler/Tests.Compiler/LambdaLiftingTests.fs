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

    let GetAdjFromCallable call =
        call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)

    let GetCtlFromCallable call =
        call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlled)

    let GetCtlAdjFromCallable call =
        call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlledAdjoint)

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

    let CheckIfLineIsCall ``namespace`` name input =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape ``namespace`` <| Regex.Escape name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
        let args = @"\(\s*(.*[^\s])?\s*\)"
        let regex = sprintf @"^\s*%s\s*%s\s*%s;$" call typeArgs args

        let regexMatch = Regex.Match(input, regex)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    let MakeApplicationRegex (opName: QsQualifiedName) =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape opName.Namespace <| Regex.Escape opName.Name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
        let args = @"\(?\s*([\w\s,\.]*)?\s*\)?"
        sprintf @"\(%s\s*%s,\s*%s\)" <| call <| typeArgs <| args

    let IsApplyIfArgMatch input resultVar (opName: QsQualifiedName) =
        let regexMatch =
            Regex.Match(input, sprintf @"^\s*%s,\s*%s$" <| Regex.Escape resultVar <| MakeApplicationRegex opName)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    let IsApplyIfElseArgsMatch input resultVar (opName1: QsQualifiedName) (opName2: QsQualifiedName) =
        let ApplyIfElseRegex =
            sprintf @"^%s,\s*%s,\s*%s$"
            <| Regex.Escape resultVar
            <| MakeApplicationRegex opName1
            <| MakeApplicationRegex opName2

        let regexMatch = Regex.Match(input, ApplyIfElseRegex)

        if regexMatch.Success then
            (true,
             regexMatch.Groups.[3].Value,
             regexMatch.Groups.[4].Value,
             regexMatch.Groups.[7].Value,
             regexMatch.Groups.[8].Value)
        else
            (false, "", "", "", "")

    let IsTypeArgsMatch input targs =
        Regex.Match(input, sprintf @"^%s$" <| Regex.Escape targs).Success

    let CheckIfSpecializationHasCalls specialization (calls: seq<int * string * string>) =
        let lines = GetLinesFromSpecialization specialization
        Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) calls

    let AssertSpecializationHasCalls specialization calls =
        Assert.True(
            CheckIfSpecializationHasCalls specialization calls,
            sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind
        )

    let ExpandBuiltInQualifiedSymbol (i, (builtin: BuiltIn)) =
        (i, builtin.FullName.Namespace, builtin.FullName.Name)

    let IdentifyGeneratedByCalls generatedCallables calls =
        let mutable callables =
            generatedCallables |> Seq.map (fun x -> x, x |> (GetBodyFromCallable >> GetLinesFromSpecialization))

        let hasCall callable (call: seq<int * string * string>) =
            let (_, lines: string []) = callable
            Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) call

        Assert.True(Seq.length callables = Seq.length calls) // This should be true if this method is called correctly

        let mutable rtrn = Seq.empty

        let removeAt i lst =
            Seq.append <| Seq.take i lst <| Seq.skip (i + 1) lst

        for call in calls do
            callables
            |> Seq.tryFindIndex (fun callSig -> hasCall callSig call)
            |> (fun x ->
                Assert.True(x <> None, "Did not find expected generated content")
                rtrn <- Seq.append rtrn [ Seq.item x.Value callables ]
                callables <- removeAt x.Value callables)

        rtrn |> Seq.map (fun (x, y) -> x)

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

    let ApplyIfElseTest compilation =
        let original = GetCallableWithName compilation Signatures.ClassicalControlNS "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." original.Parent original.Kind
        )

        let (success, targs, args) =
            CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        targs, args

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

    [<Fact>]
    [<Trait("Category", "Return Values")>]
    member this.``With Return Value``() =
        let result = CompileLambdaLiftingTest 1

        let original = GetCallableWithName result Signatures.LambdaLiftingNS "Foo" |> GetBodyFromCallable

        let generated =
            GetCallablesWithSuffix result Signatures.LambdaLiftingNS "_Foo"
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

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
            |> (fun x ->
                Assert.True(1 = Seq.length x)
                Seq.item 0 x |> GetBodyFromCallable)

        let lines = generated |> GetLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements." generated.Parent generated.Kind
        )

        Assert.True(lines.[0] = "return (x, y, z);", "The generated callable did not have the expected content.")

        lines |> Seq.map (printfn "%s") |> ignore

        let lines = original |> GetLinesFromSpecialization

        lines |> Seq.map (printfn "%s") |> ignore

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
