// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlledTransformation
open Xunit
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.SyntaxTokens


type ClassicalControlTests () =

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    do let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
       Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    let ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine ("TestCases", "LinkingTests", fileName) |> File.ReadAllText
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
        let processedCompilation = ClassicallyControlledTransformation.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] processedCompilation
        processedCompilation

    let GetBodyFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)
    let GetAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)
    let GetCtlFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlled)
    let GetCtlAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlledAdjoint)

    let GetLinesFromSpecialization specialization =
        let writer = new SyntaxTreeToQs()

        specialization
        |> fun x -> match x.Implementation with | Provided (_, body) -> Some body | _ -> None
        |> Option.get
        |> writer.Scope.Transform
        |> ignore

        (writer.Scope :?> ScopeToQs).Output.Split(Environment.NewLine)
        |> Array.filter (fun str -> str <> String.Empty)

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
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape opName.Namespace.Value <| Regex.Escape opName.Name.Value
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?"  // Does not support nested type args
        let args = @"\(\s*(.*[^\s])?\s*\)"

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

    let CheckIfSpecializationHasCalls specialization (calls : seq<int * string * string>) =
        let lines = GetLinesFromSpecialization specialization
        Seq.forall (fun (i, ns, name) -> CheckIfLineIsCall ns name lines.[i] |> (fun (x, _, _) -> x)) calls

    let AssertSpecializationHasCalls specialization calls =
        Assert.True(CheckIfSpecializationHasCalls specialization calls, sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind)

    let ExpandBuiltInQualifiedSymbol (i, (builtin : BuiltIn)) = (i, builtin.Namespace.Value, builtin.Name.Value)

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
        |> Seq.filter (fun x -> x.Name.Value = ns)
        |> GlobalCallableResolutions
        |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith suffix)
        |> Seq.map (fun x -> x.Value)

    let GetCallableWithName compilation ns name =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name.Value = ns)
        |> GlobalCallableResolutions
        |> Seq.find (fun x -> x.Key.Name.Value = name)
        |> (fun x -> x.Value)

    let ApplyIfElseTest compilation =
        let generated = GetCallablesWithSuffix compilation Signatures.ClassicalControlNs "_Foo"

        Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

        let ifContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let elseContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
            ]

        let orderedGens = IdentifyGeneratedByCalls generated [ifContent; elseContent]
        let ifOp, elseOp = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

        let original = GetCallableWithName compilation Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        args, ifOp.FullName, elseOp.FullName

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
    [<Trait("Category","Content Hoisting")>]
    member this.``Basic Hoist`` () =
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
    [<Trait("Category","Content Hoisting")>]
    member this.``Hoist Loops`` () =
        CompileClassicalControlTest 2 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Don't Hoist Single Call`` () =
        // Single calls should not be hoisted into their own operation
        CompileClassicalControlTest 3 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Hoist Single Non-Call`` () =
        // Single expressions that are not calls should be hoisted into their own operation
        CompileClassicalControlTest 4 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Don't Hoist Return Statements`` () =
        CompileClassicalControlTest 5 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``All-Or-None Hoisting`` () =
        CompileClassicalControlTest 6 |> ignore

    [<Fact>]
    [<Trait("Category","Apply If Calls")>]
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
    [<Trait("Category","Apply If Calls")>]
    member this.``Apply If Zero Else One`` () =
        let (args, ifOp, elseOp) = CompileClassicalControlTest 8 |> ApplyIfElseTest
        IsApplyIfElseArgsMatch args "r" ifOp elseOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category","Apply If Calls")>]
    member this.``Apply If One Else Zero`` () =
        let (args, ifOp, elseOp) = CompileClassicalControlTest 9 |> ApplyIfElseTest
        // The operation arguments should be swapped from the previous test
        IsApplyIfElseArgsMatch args "r" elseOp ifOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``If Elif`` () =
        let result = CompileClassicalControlTest 10

        let generated = GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"

        Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

        let ifContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let elifContent =
            [
                (0, "SubOps", "SubOp3");
                (1, "SubOps", "SubOp1");
            ]
        let elseContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
            ]

        let orderedGens = IdentifyGeneratedByCalls generated [ifContent; elifContent; elseContent]
        let ifOp, elifOp, elseOp = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

        let original = GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> GetBodyFromCallable
        let lines = original |> GetLinesFromSpecialization

        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, _, _, subArgs) = IsApplyIfElseArgsMatch args "r" ifOp.FullName { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name }
        Assert.True(success, errorMsg)
        IsApplyIfElseArgsMatch subArgs "r" elseOp.FullName elifOp.FullName // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``And Condition`` () =
        let (args, ifOp, elseOp) = CompileClassicalControlTest 11 |> ApplyIfElseTest

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, subArgs, _, _) = IsApplyIfElseArgsMatch args "r" { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name } elseOp
        Assert.True(success, errorMsg)
        IsApplyIfElseArgsMatch subArgs "r" elseOp ifOp // if and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","If Structure Reshape")>]
    member this.``Or Condition`` () =
        let (args, ifOp, elseOp) = CompileClassicalControlTest 12 |> ApplyIfElseTest

        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, _, _, subArgs) = IsApplyIfElseArgsMatch args "r" ifOp { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name }
        Assert.True(success, errorMsg)
        IsApplyIfElseArgsMatch subArgs "r" elseOp ifOp // if and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Don't Hoist Functions`` () =
        CompileClassicalControlTest 13 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Hoist Self-Contained Mutable`` () =
        CompileClassicalControlTest 14 |> ignore

    [<Fact>]
    [<Trait("Category","Content Hoisting")>]
    member this.``Don't Hoist General Mutable`` () =
        CompileClassicalControlTest 15 |> ignore

    [<Fact>]
    [<Trait("Category","Generics Support")>]
    member this.``Generics Support`` () =
        let result = CompileClassicalControlTest 16

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions
        let original  = callables
                        |> Seq.find (fun x -> x.Key.Name.Value = "Foo")
                        |> fun x -> x.Value
        let generated = callables
                        |> Seq.find (fun x -> x.Key.Name.Value.EndsWith "_Foo")
                        |> fun x -> x.Value

        let GetTypeParams call =
            call.Signature.TypeParameters
            |> Seq.choose (function | ValidName str -> Some str.Value | InvalidName -> None)

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
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfZero.Namespace.Value BuiltIn.ApplyIfZero.Name.Value lines.[1]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.FullName QsSpecializationKind.QsBody)

        let (success, typeArgs, _) = IsApplyIfArgMatch args "r" generated.FullName
        Assert.True(success, sprintf "ApplyIfZero did not have the correct arguments")

        AssertTypeArgsMatch originalTypeParams <| typeArgs.Replace("'", "").Replace(" ", "").Split(",")

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Adjoint Support`` () =
        let result = CompileClassicalControlTest 17

        let callables = result.Namespaces
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        let selfOp = callables
                     |> Seq.find (fun x -> x.Key.Name.Value = "Self")
                     |> fun x -> x.Value
        let invertOp = callables
                       |> Seq.find (fun x -> x.Key.Name.Value = "Invert")
                       |> fun x -> x.Value
        let providedOp = callables
                       |> Seq.find (fun x -> x.Key.Name.Value = "Provided")
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
                      |> Seq.find (fun x -> x.Key.Name.Value.EndsWith "_Self")
                      |> fun x -> x.Value
        let _invertOp = callables
                        |> Seq.find (fun x -> x.Key.Name.Value.EndsWith "_Invert")
                        |> fun x -> x.Value
        let _providedOps = callables
                           |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_Provided")
                           |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let adjointContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
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
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        let distributeOp = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "Distribute")
                           |> fun x -> x.Value
        let providedOp = callables
                         |> Seq.find (fun x -> x.Key.Name.Value = "Provided")
                         |> fun x -> x.Value

        [(1, BuiltIn.ApplyIfZeroC)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable distributeOp)

        [(1, BuiltIn.ApplyIfZero)]
        |> Seq.map ExpandBuiltInQualifiedSymbol
        |> AssertSpecializationHasCalls (GetBodyFromCallable providedOp)

        let _distributeOp = callables
                            |> Seq.find (fun x -> x.Key.Name.Value.EndsWith "_Distribute")
                            |> fun x -> x.Value
        let _providedOps = callables
                           |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_Provided")
                           |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let controlledContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
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
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "ProvidedBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_ProvidedBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "ProvidedControlled")
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
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_ProvidedControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "ProvidedAdjoint")
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
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_ProvidedAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]
            let ctlAdjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "ProvidedAll")
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
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_ProvidedAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(4 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
                ]
            let ctlAdjContent =
                [
                    (2, "SubOps", "SubOp3");
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
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "DistributeBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_DistributeBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "DistributeControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_DistributeControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "DistributeAdjoint")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOneC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_DistributeAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "DistributeAll")
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
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_DistributeAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
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
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "InvertBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_InvertBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "InvertControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOneA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_InvertControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "InvertAdjoint")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroCA)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetAdjFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_InvertAdjoint")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
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
                           |> Seq.find (fun x -> x.Key.Name.Value = "InvertAll")
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
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_InvertAll")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]
            let adjContent =
                [
                    (0, "SubOps", "SubOp2");
                    (1, "SubOps", "SubOp3");
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
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "SelfBody")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZeroC)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_SelfBody")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]

            let bodyGen = (Seq.item 0 generated)
            AssertSpecializationHasCalls (GetBodyFromCallable bodyGen) bodyContent

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [QsFunctor.Controlled] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables
                           |> Seq.find (fun x -> x.Key.Name.Value = "SelfControlled")
                           |> fun x -> x.Value

            [(1, BuiltIn.ApplyIfZero)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetBodyFromCallable original)

            [(1, BuiltIn.ApplyIfOne)]
            |> Seq.map ExpandBuiltInQualifiedSymbol
            |> AssertSpecializationHasCalls (GetCtlFromCallable original)

            let generated = callables
                            |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_SelfControlled")
                            |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent =
                [
                    (0, "SubOps", "SubOp1");
                    (1, "SubOps", "SubOp2");
                ]
            let ctlContent =
                [
                    (0, "SubOps", "SubOp3");
                    (1, "SubOps", "SubOp1");
                ]

            let orderedGens = IdentifyGeneratedByCalls generated [bodyContent; ctlContent]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)

            AssertCallSupportsFunctors [QsFunctor.Controlled;QsFunctor.Adjoint] original
            AssertCallSupportsFunctors [] bodyGen
            AssertCallSupportsFunctors [] ctlGen

        controlledCheck ()

    [<Fact>]
    [<Trait("Category","Functor Support")>]
    member this.``Within Block Support`` ()=
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
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let innerContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
            ]

        AssertSpecializationHasCalls original originalContent

        let orderedGens = IdentifyGeneratedByCalls generated [outerContent; innerContent]
        let outerOp = (Seq.item 0 orderedGens)

        AssertCallSupportsFunctors [QsFunctor.Adjoint] outerOp

        let lines = GetLinesFromSpecialization original
        let (success, _, args) = CheckIfLineIsCall BuiltIn.ApplyIfZeroA.Namespace.Value BuiltIn.ApplyIfZeroA.Name.Value lines.[2]
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody)

        let (success, _, _) = IsApplyIfArgMatch args "r" outerOp.FullName
        Assert.True(success, sprintf "ApplyIfZeroA did not have the correct arguments")