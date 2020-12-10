// Copyright (c) Microsoft Corporation. All rights reserved.
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
open Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit


type ClassicalControlTests () =

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine ("TestCases", fileName) |> File.ReadAllText
        sourceInput.Split ([|"==="|], StringSplitOptions.RemoveEmptyEntries)

    let BuildContent content =

        let fileId = getTempFile()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    let CompileClassicalControlTest testNumber =
        let srcChunks = ReadAndChunkSourceFile "ClassicalControl.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = BuildContent <| shared + srcChunks.[testNumber]
        let processedCompilation = ReplaceClassicalControl.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] processedCompilation
        processedCompilation

    let GetBodyFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)
    let GetAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)
    let GetCtlFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlled)
    let GetCtlAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlledAdjoint)

    let GetLinesFromSpecialization specialization =
        let writer = new SyntaxTreeToQsharp()

        specialization
        |> fun x -> match x.Implementation with | Provided (_, body) -> Some body | _ -> None
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

    let MakeApplicationRegex (opName : QsQualifiedName) =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape opName.Namespace <| Regex.Escape opName.Name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?"  // Does not support nested type args
        let args = @"\(?\s*([\w\s,\.]*)?\s*\)?"

        sprintf @"\(%s\s*%s,\s*%s\)" <| call <| typeArgs <| args

    let IsApplyIfArgMatch input resultVar (opName : QsQualifiedName) =
        let regexMatch = Regex.Match(input, sprintf @"^\s*%s,\s*%s$" <| Regex.Escape resultVar <| MakeApplicationRegex opName)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    let IsApplyIfElseArgsMatch input resultVar (opName1 : QsQualifiedName) (opName2 : QsQualifiedName) =
        let ApplyIfElseRegex = sprintf @"^%s,\s*%s,\s*%s$"
                                <| Regex.Escape resultVar
                                <| MakeApplicationRegex opName1
                                <| MakeApplicationRegex opName2

        let regexMatch = Regex.Match(input, ApplyIfElseRegex)
        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value, regexMatch.Groups.[7].Value, regexMatch.Groups.[8].Value)
        else
            (false, "", "", "", "")

    let IsTypeArgsMatch input targs = Regex.Match(input, sprintf @"^%s$" <| Regex.Escape targs).Success

    let CheckIfSpecializationHasCalls specialization (calls : seq<int * string * string>) =
        let lines = GetLinesFromSpecialization specialization
        Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) calls

    let AssertSpecializationHasCalls specialization calls =
        Assert.True(CheckIfSpecializationHasCalls specialization calls, sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind)

    let ExpandBuiltInQualifiedSymbol (i, (builtin : BuiltIn)) = (i, builtin.FullName.Namespace, builtin.FullName.Name)

    let IdentifyGeneratedByCalls generatedCallables calls =
        let mutable callables = generatedCallables |> Seq.map (fun x -> x, x |> (GetBodyFromCallable >> GetLinesFromSpecialization))
        let hasCall callable (call : seq<int * string * string>) =
            let (_, lines : string[]) = callable
            Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) call

        Assert.True(Seq.length callables = Seq.length calls) // This should be true if this method is called correctly

        let mutable rtrn = Seq.empty

        let removeAt i lst =
            Seq.append
            <| Seq.take i lst
            <| Seq.skip (i+1) lst

        for call in calls do
            callables
            |> Seq.tryFindIndex (fun callSig -> hasCall callSig call)
            |> (fun x ->
                    Assert.True (x <> None, sprintf "Did not find expected generated content")
                    rtrn <- Seq.append rtrn [Seq.item x.Value callables]
                    callables <- removeAt x.Value callables
               )
        rtrn |> Seq.map (fun (x,y) -> x)

    let GetCallablesWithSuffix compilation ns (suffix : string) =
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

        let original = GetCallableWithName compilation Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, targs, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        targs, args

    let DoesCallSupportFunctors expectedFunctors call =
        let hasAdjoint = expectedFunctors |> Seq.contains QsFunctor.Adjoint
        let hasControlled = expectedFunctors |> Seq.contains QsFunctor.Controlled

        // Checks the characteristics match
        let charMatch = lazy match call.Signature.Information.Characteristics.SupportedFunctors with
                             | Value x -> x.SetEquals(expectedFunctors)
                             | Null -> 0 = Seq.length expectedFunctors

        // Checks that the target specializations are present
        let adjMatch = lazy if hasAdjoint then
                                call.Specializations
                                |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)
                                |> function
                                   | None -> false
                                   | Some x -> match x.Implementation with
                                               | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Invert || gen = QsGeneratorDirective.SelfInverse
                                               | SpecializationImplementation.Provided _ -> true
                                               | _ -> false
                            else true

        let ctlMatch = lazy if hasControlled then
                                call.Specializations
                                |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsControlled)
                                |> function
                                   | None -> false
                                   | Some x -> match x.Implementation with
                                               | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Distribute
                                               | SpecializationImplementation.Provided _ -> true
                                               | _ -> false
                            else true

        charMatch.Value && adjMatch.Value && ctlMatch.Value

    let AssertCallSupportsFunctors expectedFunctors call =
        Assert.True(DoesCallSupportFunctors expectedFunctors call, sprintf "Callable %O did not support the expected functors" call.FullName)

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Basic Lift`` () =
        let result = CompileClassicalControlTest 1

        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        [
            (0, "SubOps", "SubOp1");
            (1, "SubOps", "SubOp2");
            (2, "SubOps", "SubOp3");
        ]
        |> AssertSpecializationHasCalls generated

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Loops`` () =
        CompileClassicalControlTest 2 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Don't Lift Single Call`` () =
        // Single calls should not be lifted into their own operation
        CompileClassicalControlTest 3 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Single Non-Call`` () =
        // Single expressions that are not calls should be lifted into their own operation
        CompileClassicalControlTest 4 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Don't Lift Return Statements`` () =
        CompileClassicalControlTest 5 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``All-Or-None Lifting`` () =
        CompileClassicalControlTest 6 |> ignore

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``ApplyIfZero And ApplyIfOne`` () =
        let result = CompileClassicalControlTest 7

        let originalOp = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable

        [
            (1, BuiltIn.ApplyIfZero);
            (3, BuiltIn.ApplyIfOne);
        ]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Apply If Zero Else One`` () =
        let (targs, args) = CompileClassicalControlTest 8 |> ApplyIfElseTest

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        IsApplyIfElseArgsMatch args "r" Bar SubOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Result, Unit", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Apply If One Else Zero`` () =
        let (targs, args) = CompileClassicalControlTest 9 |> ApplyIfElseTest

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        // The operation arguments should be swapped from the previous test
        IsApplyIfElseArgsMatch args "r" SubOp1 Bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Unit, Result", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``If Elif`` () =
        let result  = CompileClassicalControlTest 10

        let ifOp = {Namespace = "SubOps"; Name = "SubOp1"}
        let elifOp = {Namespace = "SubOps"; Name = "SubOp2"}
        let elseOp = {Namespace = "SubOps"; Name = "SubOp3"}
        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        let lines = original |> GetLinesFromSpecialization
        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        IsApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> GetLinesFromSpecialization
        Assert.True(1 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[0]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        IsApplyIfElseArgsMatch args "r" elseOp elifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``And Condition`` () =
        let result  = CompileClassicalControlTest 11

        let ifOp = {Namespace = "SubOps"; Name = "SubOp1"}
        let elseOp = {Namespace = "SubOps"; Name = "SubOp2"}
        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        let lines = original |> GetLinesFromSpecialization
        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        IsApplyIfElseArgsMatch args "r" generated.Parent elseOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> GetLinesFromSpecialization
        Assert.True(1 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[0]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        IsApplyIfElseArgsMatch args "r" elseOp ifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``Or Condition`` () =
        let result  = CompileClassicalControlTest 12

        let ifOp = {Namespace = "SubOps"; Name = "SubOp1"}
        let elseOp = {Namespace = "SubOps"; Name = "SubOp2"}
        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        let lines = original |> GetLinesFromSpecialization
        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        IsApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> GetLinesFromSpecialization
        Assert.True(1 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[0]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        IsApplyIfElseArgsMatch args "r" elseOp ifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Don't Lift Functions`` () =
        CompileClassicalControlTest 13 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Self-Contained Mutable`` () =
        CompileClassicalControlTest 14 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Don't Lift General Mutable`` () =
        CompileClassicalControlTest 15 |> ignore

    [<Fact>]
    [<Trait("Category","Generics Support")>]
    member this.``Generics Support`` () =
        let result = CompileClassicalControlTest 16

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions
        let original  = callables
                        |> Seq.find (fun x -> x.Key.Name = "Foo")
                        |> fun x -> x.Value
        let generated = callables
                        |> Seq.find (fun x -> x.Key.Name.EndsWith "_Foo")
                        |> fun x -> x.Value

        let GetTypeParams call =
            call.Signature.TypeParameters
            |> Seq.choose (function | ValidName str -> Some str | InvalidName -> None)

        let AssertTypeArgsMatch typeArgs1 typeArgs2 =
            let errorMsg = "The type parameters for the original and generated operations do not match"
            Assert.True(Seq.length typeArgs1 = Seq.length typeArgs2, errorMsg)

            for pair in Seq.zip typeArgs1 typeArgs2 do
                Assert.True(fst pair = snd pair, errorMsg)

        // Assert that the generated operation has the same type parameters as the original operation
        let originalTypeParams = GetTypeParams original
        let generatedTypeParams = GetTypeParams generated
        AssertTypeArgsMatch originalTypeParams generatedTypeParams

        // Assert that the original operation calls the generated operation with the appropriate type arguments
        let lines = GetBodyFromCallable original |> GetLinesFromSpecialization
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfZero.FullName.Namespace BuiltIn.ApplyIfZero.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.FullName QsSpecializationKind.QsBody)

        let (success, typeArgs, _) = IsApplyIfArgMatch args "r" generated.FullName
        Assert.True(success, sprintf "ApplyIfZero did not have the correct arguments")

        AssertTypeArgsMatch originalTypeParams <| typeArgs.Replace("'", "").Replace(" ", "").Split(",")

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Adjoint Support`` () =
        let result = CompileClassicalControlTest 17

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        let selfOp = callables
                     |> Seq.find (fun x -> x.Key.Name = "Self")
                     |> fun x -> x.Value
        let invertOp = callables
                       |> Seq.find (fun x -> x.Key.Name = "Invert")
                       |> fun x -> x.Value
        let providedOp = callables
                       |> Seq.find (fun x -> x.Key.Name = "Provided")
                       |> fun x -> x.Value

        [(1, BuiltIn.ApplyIfZero)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable selfOp)

        [(1, BuiltIn.ApplyIfZeroA)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable invertOp)

        [(1, BuiltIn.ApplyIfZero)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable providedOp)

        let _selfOp = callables
                      |> Seq.find (fun x -> x.Key.Name.EndsWith "_Self")
                      |> fun x -> x.Value
        let _invertOp = callables
                        |> Seq.find (fun x -> x.Key.Name.EndsWith "_Invert")
                        |> fun x -> x.Value
        let _providedOps = callables
                           |> Seq.filter (fun x -> x.Key.Name.EndsWith "_Provided")
                           |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent =
            [
                (0, "SubOps", "SubOpCA1");
                (1, "SubOps", "SubOpCA2");
            ]
        let adjointContent =
            [
                (0, "SubOps", "SubOpCA2");
                (1, "SubOps", "SubOpCA3");
            ]

        let orderedGens = IdentifyGeneratedByCalls _providedOps [bodyContent; adjointContent]
        let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

        AssertCallSupportsFunctors [] _selfOp
        AssertCallSupportsFunctors [QsFunctor.Adjoint] _invertOp
        AssertCallSupportsFunctors [] bodyGen
        AssertCallSupportsFunctors [] adjGen

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Controlled Support`` () =
        let result = CompileClassicalControlTest 18

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        let distributeOp = callables
                           |> Seq.find (fun x -> x.Key.Name = "Distribute")
                           |> fun x -> x.Value
        let providedOp = callables
                         |> Seq.find (fun x -> x.Key.Name = "Provided")
                         |> fun x -> x.Value

        [(1, BuiltIn.ApplyIfZeroC)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable distributeOp)

        [(1, BuiltIn.ApplyIfZero)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable providedOp)

        let _distributeOp = callables
                            |> Seq.find (fun x -> x.Key.Name.EndsWith "_Distribute")
                            |> fun x -> x.Value
        let _providedOps = callables
                           |> Seq.filter (fun x -> x.Key.Name.EndsWith "_Provided")
                           |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent =
            [
                (0, "SubOps", "SubOpCA1");
                (1, "SubOps", "SubOpCA2");
            ]
        let controlledContent =
            [
                (0, "SubOps", "SubOpCA2");
                (1, "SubOps", "SubOpCA3");
            ]

        let orderedGens = IdentifyGeneratedByCalls _providedOps [bodyContent; controlledContent]
        let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

        AssertCallSupportsFunctors [QsFunctor.Controlled] _distributeOp
        AssertCallSupportsFunctors [] bodyGen
        AssertCallSupportsFunctors [] ctlGen

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Controlled Adjoint Support - Provided`` () =
        let result = CompileClassicalControlTest 19

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "ProvidedBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlAdjContent]
            let bodyGen, ctlAdjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen
            AssertCallSupportsFunctors [] ctlAdjGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "ProvidedControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent; ctlAdjContent]
            let bodyGen, ctlGen, ctlAdjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Adjoint] bodyGen
            AssertCallSupportsFunctors [] ctlGen
            AssertCallSupportsFunctors [] ctlAdjGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "ProvidedAdjoint")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; adjContent; ctlAdjContent]
            let bodyGen, adjGen, ctlAdjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled] bodyGen
            AssertCallSupportsFunctors [] adjGen
            AssertCallSupportsFunctors [] ctlAdjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "ProvidedAll")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZero)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(4 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]
            let ctlAdjContent =
                [
                    (2, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent; adjContent; ctlAdjContent]
            let bodyGen, ctlGen, adjGen, ctlAdjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens), (Seq.item 3 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [] bodyGen
            AssertCallSupportsFunctors [] ctlGen
            AssertCallSupportsFunctors [] adjGen
            AssertCallSupportsFunctors [] ctlAdjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Controlled Adjoint Support - Distribute`` ()=
        let result = CompileClassicalControlTest 20

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "DistributeBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "DistributeControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen
            AssertCallSupportsFunctors [] ctlGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "DistributeAdjoint")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOneC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; adjContent]
            let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled] bodyGen
            AssertCallSupportsFunctors [QsFunctor.Controlled] adjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "DistributeAll")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZero)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            [(1, BuiltIn.ApplyIfOneC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent; adjContent]
            let bodyGen, ctlGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [] bodyGen
            AssertCallSupportsFunctors [] ctlGen
            AssertCallSupportsFunctors [QsFunctor.Controlled] adjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Controlled Adjoint Support - Invert`` ()=
        let result = CompileClassicalControlTest 21

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "InvertBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "InvertControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOneA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Adjoint] bodyGen
            AssertCallSupportsFunctors [QsFunctor.Adjoint] ctlGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "InvertAdjoint")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; adjContent]
            let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen
            AssertCallSupportsFunctors [] adjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "InvertAll")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZero)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOneA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOpCA2");
                    (1, "SubOps", "SubOpCA3");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent; adjContent]
            let bodyGen, ctlGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [] bodyGen
            AssertCallSupportsFunctors [QsFunctor.Adjoint] ctlGen
            AssertCallSupportsFunctors [] adjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Controlled Adjoint Support - Self`` ()=
        let result = CompileClassicalControlTest 22

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "SelfBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_SelfBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name = "SelfControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZero)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.EndsWith "_SelfControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOpCA1");
                    (1, "SubOps", "SubOpCA2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOpCA3");
                    (1, "SubOps", "SubOpCA1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [] bodyGen
            AssertCallSupportsFunctors [] ctlGen

        controlledCheck ()

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Within Block Support`` () =
        let result = CompileClassicalControlTest 23

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"

        Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

        let originalContent =
            [
                (2, BuiltIn.ApplyIfZeroA);
                (5, BuiltIn.ApplyIfOne);
            ] |> Seq.map ExpandBuiltInQualifiedSymbol
        let outerContent =
            [
                (0, "SubOps", "SubOpCA1");
                (1, "SubOps", "SubOpCA2");
            ]
        let innerContent =
            [
                (0, "SubOps", "SubOpCA2");
                (1, "SubOps", "SubOpCA3");
            ]

        AssertSpecializationHasCalls original originalContent

        let orderedGens = IdentifyGeneratedByCalls generated [outerContent; innerContent]
        let outerOp = (Seq.item 0 orderedGens)

        AssertCallSupportsFunctors [QsFunctor.Adjoint] outerOp

        let lines = GetLinesFromSpecialization original
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfZeroA.FullName.Namespace BuiltIn.ApplyIfZeroA.FullName.Name lines.[2]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody)

        let (success, _, _) = IsApplyIfArgMatch args "r" outerOp.FullName
        Assert.True(success, "ApplyIfZeroA did not have the correct arguments")

    [<Fact>]
    [<Trait("Category","Generics Support")>]
    member this.``Arguments Partially Resolve Type Parameters`` () =
        let result = CompileClassicalControlTest 24

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable

        let lines = GetLinesFromSpecialization original
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfZero.FullName.Namespace BuiltIn.ApplyIfZero.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody)

        let (success, typeArgs, _) = IsApplyIfArgMatch args "r" {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        Assert.True((typeArgs = "Int, Double"), "Bar did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Functor Application`` () =
        CompileClassicalControlTest 25 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Partial Application`` () =
        CompileClassicalControlTest 26 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift Array Item Call`` () =
        CompileClassicalControlTest 27 |> ignore

    [<Fact>]
    [<Trait("Category","Content Lifting")>]
    member this.``Lift One Not Both`` () =
        // If lifting is not needed on one of the blocks, it should not
        // prevent the other blocks from being lifted, as it would in
        // the All-Or-Nothing test where a block is *invalid* for
        // lifting due to a set statement or return statement.
        CompileClassicalControlTest 28 |> ignore

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Apply Conditionally`` () =
        let result = CompileClassicalControlTest 29

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(3 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, targs, args) = CheckIfLineIsCall BuiltIn.ApplyConditionally.FullName.Namespace BuiltIn.ApplyConditionally.FullName.Name lines.[2]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        IsApplyIfElseArgsMatch args "[r1], [r2]" Bar SubOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Result, Unit", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Apply Conditionally With NoOp`` () =
        let result = CompileClassicalControlTest 30

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(3 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, targs, args) = CheckIfLineIsCall BuiltIn.ApplyConditionally.FullName.Namespace BuiltIn.ApplyConditionally.FullName.Name lines.[2]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let NoOp = {Namespace = "Microsoft.Quantum.Canon"; Name = "NoOp"}

        IsApplyIfElseArgsMatch args "[r1], [r2]" Bar NoOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Result, Unit", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Inequality Condition")>]
    member this.``Inequality with ApplyConditionally`` () =
        let result = CompileClassicalControlTest 31

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(3 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, targs, args) = CheckIfLineIsCall BuiltIn.ApplyConditionally.FullName.Namespace BuiltIn.ApplyConditionally.FullName.Name lines.[2]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        IsApplyIfElseArgsMatch args "[r1], [r2]" SubOp1 Bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Unit, Result", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Inequality Condition")>]
    member this.``Inequality with Apply If One Else Zero`` () =
        let (targs, args) = CompileClassicalControlTest 32 |> ApplyIfElseTest

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        IsApplyIfElseArgsMatch args "r" SubOp1 Bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Unit, Result", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Inequality Condition")>]
    member this.``Inequality with Apply If Zero Else One`` () =
        let (targs, args) = CompileClassicalControlTest 33 |> ApplyIfElseTest

        let Bar = {Namespace = Signatures.ClassicalControlNs; Name = "Bar"}
        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}

        IsApplyIfElseArgsMatch args "r" Bar SubOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(IsTypeArgsMatch targs "Result, Unit", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category","Inequality Condition")>]
    member this.``Inequality with ApplyIfOne`` () =
        let result = CompileClassicalControlTest 34

        let originalOp = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable

        [ (1, BuiltIn.ApplyIfOne) ]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category","Inequality Condition")>]
    member this.``Inequality with ApplyIfZero`` () =
        let result = CompileClassicalControlTest 35

        let originalOp = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable

        [ (1, BuiltIn.ApplyIfZero) ]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Literal on the Left`` () =
        let result = CompileClassicalControlTest 36

        let originalOp = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable

        [(1, BuiltIn.ApplyIfZero)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Simple NOT condition`` () =
        let (_, args) = CompileClassicalControlTest 37 |> ApplyIfElseTest

        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}
        let SubOp2 = {Namespace = "SubOps"; Name = "SubOp2"}

        IsApplyIfElseArgsMatch args "r" SubOp2 SubOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Outer NOT condition`` () =
        let result  = CompileClassicalControlTest 38

        let ifOp = {Namespace = "SubOps"; Name = "SubOp1"}
        let elseOp = {Namespace = "SubOps"; Name = "SubOp2"}
        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        let lines = original |> GetLinesFromSpecialization
        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        IsApplyIfElseArgsMatch args "r" elseOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> GetLinesFromSpecialization
        Assert.True(1 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[0]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        IsApplyIfElseArgsMatch args "r" ifOp elseOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``Nested NOT condition`` () =
        let result  = CompileClassicalControlTest 39

        let ifOp = {Namespace = "SubOps"; Name = "SubOp1"}
        let elseOp = {Namespace = "SubOps"; Name = "SubOp2"}
        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> GetBodyFromCallable)

        let lines = original |> GetLinesFromSpecialization
        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        IsApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> GetLinesFromSpecialization
        Assert.True(1 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind)
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.FullName.Namespace BuiltIn.ApplyIfElseR.FullName.Name lines.[0]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        IsApplyIfElseArgsMatch args "r" ifOp elseOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Condition API Conversion")>]
    member this.``One-sided NOT condition`` () =
        let (_, args) = CompileClassicalControlTest 40 |> ApplyIfElseTest

        let SubOp1 = {Namespace = "SubOps"; Name = "SubOp1"}
        let NoOp = {Namespace = "Microsoft.Quantum.Canon"; Name = "NoOp"}

        IsApplyIfElseArgsMatch args "r" SubOp1 NoOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

