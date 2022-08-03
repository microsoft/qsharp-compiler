// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlled
open Xunit


type ClassicalControlTests() =

    let compileClassicalControlTest testNumber =
        let srcChunks = TestUtils.readAndChunkSourceFile "ClassicalControl.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = TestUtils.buildContent <| shared + srcChunks.[testNumber]
        let processedCompilation = ReplaceClassicalControl.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation

        Signatures.SignatureCheck
            [ Signatures.ClassicalControlNS ]
            Signatures.ClassicalControlSignatures.[testNumber - 1]
            processedCompilation

        processedCompilation

    let makeApplicationRegex (opName: QsQualifiedName) =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape opName.Namespace <| Regex.Escape opName.Name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
        let args = @"\(?\s*([\w\s,\.]*)?\s*\)?"
        sprintf @"\(%s\s*%s,\s*%s\)" <| call <| typeArgs <| args

    let checkIfStringIsCall (name: QsQualifiedName) input =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape name.Namespace <| Regex.Escape name.Name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
        let args = @"\(\s*(.*[^\s])?\s*\)"
        let regex = sprintf @"^\s*%s\s*%s\s*%s;$" call typeArgs args

        let regexMatch = Regex.Match(input, regex)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    let identifyGeneratedByCalls generatedCallables calls =
        let mutable callables =
            generatedCallables
            |> Seq.map (fun x -> x, x |> (TestUtils.getBodyFromCallable >> TestUtils.getLinesFromSpecialization))

        let hasCall callable call =
            let (_, lines: string []) = callable
            call |> Seq.forall (fun (i, name) -> checkIfStringIsCall name lines.[i] |> (fun (x, _, _) -> x))

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

        rtrn |> Seq.map (fun (x, _) -> x)

    let checkIfSpecializationHasCalls specialization (calls: seq<int * QsQualifiedName>) =
        let lines = TestUtils.getLinesFromSpecialization specialization
        Seq.forall (fun (i, name) -> checkIfStringIsCall name lines.[i] |> (fun (x, _, _) -> x)) calls

    let assertSpecializationHasCalls specialization calls =
        Assert.True(
            checkIfSpecializationHasCalls specialization calls,
            sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind
        )

    let isApplyIfArgMatch input resultVar (opName: QsQualifiedName) =
        let regexMatch =
            Regex.Match(input, sprintf @"^\s*%s,\s*%s$" <| Regex.Escape resultVar <| makeApplicationRegex opName)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    let isApplyIfElseArgsMatch input resultVar (opName1: QsQualifiedName) (opName2: QsQualifiedName) =
        let applyIfElseRegex =
            sprintf @"^%s,\s*%s,\s*%s$"
            <| Regex.Escape resultVar
            <| makeApplicationRegex opName1
            <| makeApplicationRegex opName2

        let regexMatch = Regex.Match(input, applyIfElseRegex)

        if regexMatch.Success then
            (true,
             regexMatch.Groups.[3].Value,
             regexMatch.Groups.[4].Value,
             regexMatch.Groups.[7].Value,
             regexMatch.Groups.[8].Value)
        else
            (false, "", "", "", "")

    let isTypeArgsMatch input targs =
        Regex.Match(input, sprintf @"^%s$" <| Regex.Escape targs).Success

    let applyIfElseTest compilation =
        let original =
            TestUtils.getCallableWithName compilation Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, targs, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        targs, args

    let subOp1 = { Namespace = "SubOps"; Name = "SubOp1" }
    let subOp2 = { Namespace = "SubOps"; Name = "SubOp2" }
    let subOp3 = { Namespace = "SubOps"; Name = "SubOp3" }
    let subOpCA1 = { Namespace = "SubOps"; Name = "SubOpCA1" }
    let subOpCA2 = { Namespace = "SubOps"; Name = "SubOpCA2" }
    let subOpCA3 = { Namespace = "SubOps"; Name = "SubOpCA3" }

    let bar = { Namespace = Signatures.ClassicalControlNS; Name = "Bar" }
    let noOp = { Namespace = "Microsoft.Quantum.Canon"; Name = "NoOp" }

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Basic Lift``() =
        let result = compileClassicalControlTest 1

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        [ (0, subOp1); (1, subOp2); (2, subOp3) ] |> assertSpecializationHasCalls generated

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Loops``() = compileClassicalControlTest 2 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Don't Lift Single Call``() =
        // Single calls should not be lifted into their own operation
        compileClassicalControlTest 3 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Single Non-Call``() =
        // Single expressions that are not calls should be lifted into their own operation
        compileClassicalControlTest 4 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Don't Lift Return Statements``() = compileClassicalControlTest 5 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``All-Or-None Lifting``() = compileClassicalControlTest 6 |> ignore

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``ApplyIfZero And ApplyIfOne``() =
        let result = compileClassicalControlTest 7

        let originalOp =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        [ (1, BuiltIn.ApplyIfZero.FullName); (3, BuiltIn.ApplyIfOne.FullName) ]
        |> assertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Apply If Zero Else One``() =
        let (targs, args) = compileClassicalControlTest 8 |> applyIfElseTest

        let bar = { Namespace = Signatures.ClassicalControlNS; Name = "Bar" }

        isApplyIfElseArgsMatch args "r" bar subOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Result, Unit", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Apply If One Else Zero``() =
        let (targs, args) = compileClassicalControlTest 9 |> applyIfElseTest

        let bar = { Namespace = Signatures.ClassicalControlNS; Name = "Bar" }

        // The operation arguments should be swapped from the previous test
        isApplyIfElseArgsMatch args "r" subOp1 bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Unit, Result", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "If Structure Reshape")>]
    member this.``If Elif``() =
        let result = compileClassicalControlTest 10
        let ifOp = subOp1
        let elifOp = subOp2
        let elseOp = subOp3

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"

        isApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[0]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        isApplyIfElseArgsMatch args "r" elseOp elifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category", "If Structure Reshape")>]
    member this.``And Condition``() =
        let result = compileClassicalControlTest 11
        let ifOp = subOp1
        let elseOp = subOp2

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"

        isApplyIfElseArgsMatch args "r" generated.Parent elseOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[0]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        isApplyIfElseArgsMatch args "r" elseOp ifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category", "If Structure Reshape")>]
    member this.``Or Condition``() =
        let result = compileClassicalControlTest 12
        let ifOp = subOp1
        let elseOp = subOp2

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"

        isApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[0]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        isApplyIfElseArgsMatch args "r" elseOp ifOp // elif and else are swapped because second condition is against One
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Don't Lift Functions``() =
        compileClassicalControlTest 13 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Self-Contained Mutable``() =
        compileClassicalControlTest 14 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Don't Lift General Mutable``() =
        compileClassicalControlTest 15 |> ignore

    [<Fact>]
    [<Trait("Category", "Generics Support")>]
    member this.``Generics Support``() =
        let result = compileClassicalControlTest 16

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        let original = callables |> Seq.find (fun x -> x.Key.Name = "Foo") |> (fun x -> x.Value)
        let generated = callables |> Seq.find (fun x -> x.Key.Name.EndsWith "_Foo") |> (fun x -> x.Value)

        let getTypeParams call =
            call.Signature.TypeParameters
            |> Seq.choose (function
                | ValidName str -> Some str
                | InvalidName -> None)

        let assertTypeArgsMatch typeArgs1 typeArgs2 =
            let errorMsg = "The type parameters for the original and generated operations do not match"
            Assert.True(Seq.length typeArgs1 = Seq.length typeArgs2, errorMsg)

            for pair in Seq.zip typeArgs1 typeArgs2 do
                Assert.True(fst pair = snd pair, errorMsg)

        // Assert that the generated operation has the same type parameters as the original operation
        let originalTypeParams = getTypeParams original
        let generatedTypeParams = getTypeParams generated
        assertTypeArgsMatch originalTypeParams generatedTypeParams

        // Assert that the original operation calls the generated operation with the appropriate type arguments
        let lines = TestUtils.getBodyFromCallable original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[1]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.FullName QsSpecializationKind.QsBody
        )

        let (success, typeArgs, _) = isApplyIfArgMatch args "r" generated.FullName
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        assertTypeArgsMatch originalTypeParams <| typeArgs.Replace("'", "").Replace(" ", "").Split(",")

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Adjoint Support``() =
        let result = compileClassicalControlTest 17

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        let selfOp = callables |> Seq.find (fun x -> x.Key.Name = "Self") |> (fun x -> x.Value)
        let invertOp = callables |> Seq.find (fun x -> x.Key.Name = "Invert") |> (fun x -> x.Value)
        let providedOp = callables |> Seq.find (fun x -> x.Key.Name = "Provided") |> (fun x -> x.Value)

        [ (1, BuiltIn.ApplyIfZero.FullName) ]
        |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable selfOp)

        [ (1, BuiltIn.ApplyIfZeroA.FullName) ]
        |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable invertOp)

        [ (1, BuiltIn.ApplyIfZero.FullName) ]
        |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable providedOp)

        let _selfOp = callables |> Seq.find (fun x -> x.Key.Name.EndsWith "_Self") |> (fun x -> x.Value)
        let _invertOp = callables |> Seq.find (fun x -> x.Key.Name.EndsWith "_Invert") |> (fun x -> x.Value)

        let _providedOps =
            callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_Provided") |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
        let adjointContent = [ (0, subOpCA2); (1, subOpCA3) ]
        let orderedGens = identifyGeneratedByCalls _providedOps [ bodyContent; adjointContent ]
        let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
        TestUtils.assertCallSupportsFunctors [] _selfOp
        TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] _invertOp
        TestUtils.assertCallSupportsFunctors [] bodyGen
        TestUtils.assertCallSupportsFunctors [] adjGen

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Controlled Support``() =
        let result = compileClassicalControlTest 18

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        let distributeOp = callables |> Seq.find (fun x -> x.Key.Name = "Distribute") |> (fun x -> x.Value)
        let providedOp = callables |> Seq.find (fun x -> x.Key.Name = "Provided") |> (fun x -> x.Value)

        [ (1, BuiltIn.ApplyIfZeroC.FullName) ]
        |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable distributeOp)

        [ (1, BuiltIn.ApplyIfZero.FullName) ]
        |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable providedOp)

        let _distributeOp =
            callables |> Seq.find (fun x -> x.Key.Name.EndsWith "_Distribute") |> (fun x -> x.Value)

        let _providedOps =
            callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_Provided") |> Seq.map (fun x -> x.Value)

        Assert.True(2 = Seq.length _providedOps) // Should already be asserted by the signature check

        let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
        let controlledContent = [ (0, subOpCA2); (1, subOpCA3) ]
        let orderedGens = identifyGeneratedByCalls _providedOps [ bodyContent; controlledContent ]
        let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
        TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] _distributeOp
        TestUtils.assertCallSupportsFunctors [] bodyGen
        TestUtils.assertCallSupportsFunctors [] ctlGen

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Controlled Adjoint Support - Provided``() =
        let result = compileClassicalControlTest 19

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "ProvidedBody") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroCA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlAdjFromCallable original)

            let generated =
                callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedBody") |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlAdjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlAdjContent ]
            let bodyGen, ctlAdjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlAdjGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "ProvidedControlled") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlAdjFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedControlled")
                |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let ctlAdjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent; ctlAdjContent ]

            let bodyGen, ctlGen, ctlAdjGen =
                (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlGen
            TestUtils.assertCallSupportsFunctors [] ctlAdjGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "ProvidedAdjoint") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroC.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlAdjFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedAdjoint")
                |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let adjContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let ctlAdjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; adjContent; ctlAdjContent ]

            let bodyGen, adjGen, ctlAdjGen =
                (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens)

            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] bodyGen
            TestUtils.assertCallSupportsFunctors [] adjGen
            TestUtils.assertCallSupportsFunctors [] ctlAdjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "ProvidedAll") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZero.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlAdjFromCallable original)

            let generated =
                callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_ProvidedAll") |> Seq.map (fun x -> x.Value)

            Assert.True(4 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let adjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let ctlAdjContent = [ (2, subOpCA3) ]

            let orderedGens =
                identifyGeneratedByCalls generated [ bodyContent; ctlContent; adjContent; ctlAdjContent ]

            let bodyGen, ctlGen, adjGen, ctlAdjGen =
                (Seq.item 0 orderedGens), (Seq.item 1 orderedGens), (Seq.item 2 orderedGens), (Seq.item 3 orderedGens)

            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlGen
            TestUtils.assertCallSupportsFunctors [] adjGen
            TestUtils.assertCallSupportsFunctors [] ctlAdjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Controlled Adjoint Support - Distribute``() =
        let result = compileClassicalControlTest 20

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "DistributeBody") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroCA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeBody")
                |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let bodyGen = (Seq.item 0 generated)
            assertSpecializationHasCalls (TestUtils.getBodyFromCallable bodyGen) bodyContent
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original =
                callables |> Seq.find (fun x -> x.Key.Name = "DistributeControlled") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroCA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeControlled")
                |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent ]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "DistributeAdjoint") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroC.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOneC.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeAdjoint")
                |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let adjContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; adjContent ]
            let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] bodyGen
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] adjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "DistributeAll") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZero.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            [ (1, BuiltIn.ApplyIfOneC.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_DistributeAll")
                |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let adjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent; adjContent ]
            let bodyGen, ctlGen, adjGen = Seq.item 0 orderedGens, Seq.item 1 orderedGens, Seq.item 2 orderedGens
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlGen
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] adjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Controlled Adjoint Support - Invert``() =
        let result = compileClassicalControlTest 21

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "InvertBody") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroCA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            let generated =
                callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertBody") |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let bodyGen = (Seq.item 0 generated)
            assertSpecializationHasCalls (TestUtils.getBodyFromCallable bodyGen) bodyContent
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "InvertControlled") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOneA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertControlled")
                |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent ]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] bodyGen
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] ctlGen

        controlledCheck ()

        (*-----------------------------------------*)

        let adjointCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "InvertAdjoint") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroCA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertAdjoint")
                |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let adjContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; adjContent ]
            let bodyGen, adjGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] bodyGen
            TestUtils.assertCallSupportsFunctors [] adjGen

        adjointCheck ()

        (*-----------------------------------------*)

        let allCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "InvertAll") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZero.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOneA.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getAdjFromCallable original)

            let generated =
                callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_InvertAll") |> Seq.map (fun x -> x.Value)

            Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let adjContent = [ (0, subOpCA2); (1, subOpCA3) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent; adjContent ]
            let bodyGen, ctlGen, adjGen = Seq.item 0 orderedGens, Seq.item 1 orderedGens, Seq.item 2 orderedGens
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [] bodyGen
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] ctlGen
            TestUtils.assertCallSupportsFunctors [] adjGen

        allCheck ()

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Controlled Adjoint Support - Self``() =
        let result = compileClassicalControlTest 22

        let callables =
            result.Namespaces
            |> Seq.filter (fun x -> x.Name = Signatures.ClassicalControlNS)
            |> GlobalCallableResolutions

        (*-----------------------------------------*)

        let bodyCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "SelfBody") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZeroC.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            let generated =
                callables |> Seq.filter (fun x -> x.Key.Name.EndsWith "_SelfBody") |> Seq.map (fun x -> x.Value)

            Assert.True(1 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let bodyGen = (Seq.item 0 generated)
            assertSpecializationHasCalls (TestUtils.getBodyFromCallable bodyGen) bodyContent
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled ] bodyGen

        bodyCheck ()

        (*-----------------------------------------*)

        let controlledCheck () =
            let original = callables |> Seq.find (fun x -> x.Key.Name = "SelfControlled") |> (fun x -> x.Value)

            [ (1, BuiltIn.ApplyIfZero.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getBodyFromCallable original)

            [ (1, BuiltIn.ApplyIfOne.FullName) ]
            |> assertSpecializationHasCalls (TestUtils.getCtlFromCallable original)

            let generated =
                callables
                |> Seq.filter (fun x -> x.Key.Name.EndsWith "_SelfControlled")
                |> Seq.map (fun x -> x.Value)

            Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

            let bodyContent = [ (0, subOpCA1); (1, subOpCA2) ]
            let ctlContent = [ (0, subOpCA3); (1, subOpCA1) ]
            let orderedGens = identifyGeneratedByCalls generated [ bodyContent; ctlContent ]
            let bodyGen, ctlGen = (Seq.item 0 orderedGens), (Seq.item 1 orderedGens)
            TestUtils.assertCallSupportsFunctors [ QsFunctor.Controlled; QsFunctor.Adjoint ] original
            TestUtils.assertCallSupportsFunctors [] bodyGen
            TestUtils.assertCallSupportsFunctors [] ctlGen

        controlledCheck ()

    [<Fact>]
    [<Trait("Category", "Functor Support")>]
    member this.``Within Block Support``() =
        let result = compileClassicalControlTest 23

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated = TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"

        Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

        let originalContent = [ (2, BuiltIn.ApplyIfZeroA.FullName); (5, BuiltIn.ApplyIfOne.FullName) ]

        let outerContent = [ (0, subOpCA1); (1, subOpCA2) ]
        let innerContent = [ (0, subOpCA2); (1, subOpCA3) ]

        assertSpecializationHasCalls original originalContent

        let orderedGens = identifyGeneratedByCalls generated [ outerContent; innerContent ]
        let outerOp = (Seq.item 0 orderedGens)

        TestUtils.assertCallSupportsFunctors [ QsFunctor.Adjoint ] outerOp

        let lines = TestUtils.getLinesFromSpecialization original

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZeroA.FullName lines.[2]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "r" outerOp.FullName
        Assert.True(success, "ApplyIfZeroA did not have the correct arguments")

    [<Fact>]
    [<Trait("Category", "Generics Support")>]
    member this.``Arguments Partially Resolve Type Parameters``() =
        let result = compileClassicalControlTest 24

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = TestUtils.getLinesFromSpecialization original

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[1]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, typeArgs, _) = isApplyIfArgMatch args "r" bar

        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        Assert.True((typeArgs = "Int, Double"), "Bar did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Functor Application``() =
        compileClassicalControlTest 25 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Partial Application``() =
        compileClassicalControlTest 26 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift Array Item Call``() =
        compileClassicalControlTest 27 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Lift One Not Both``() =
        // If lifting is not needed on one of the blocks, it should not
        // prevent the other blocks from being lifted, as it would in
        // the All-Or-Nothing test where a block is *invalid* for
        // lifting due to a set statement or return statement.
        compileClassicalControlTest 28 |> ignore

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Apply Conditionally``() =
        let result = compileClassicalControlTest 29

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            3 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, targs, args) = checkIfStringIsCall BuiltIn.ApplyConditionally.FullName lines.[2]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        isApplyIfElseArgsMatch args "[r1], [r2]" bar subOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Result, Unit", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Apply Conditionally With NoOp``() =
        let result = compileClassicalControlTest 30

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            3 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, targs, args) = checkIfStringIsCall BuiltIn.ApplyConditionally.FullName lines.[2]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        isApplyIfElseArgsMatch args "[r1], [r2]" bar noOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Result, Unit", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Inequality Condition")>]
    member this.``Inequality with ApplyConditionally``() =
        let result = compileClassicalControlTest 31

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            3 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, targs, args) = checkIfStringIsCall BuiltIn.ApplyConditionally.FullName lines.[2]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        isApplyIfElseArgsMatch args "[r1], [r2]" subOp1 bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyConditionally did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Unit, Result", "ApplyConditionally did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Inequality Condition")>]
    member this.``Inequality with Apply If One Else Zero``() =
        let (targs, args) = compileClassicalControlTest 32 |> applyIfElseTest

        isApplyIfElseArgsMatch args "r" subOp1 bar
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Unit, Result", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Inequality Condition")>]
    member this.``Inequality with Apply If Zero Else One``() =
        let (targs, args) = compileClassicalControlTest 33 |> applyIfElseTest

        isApplyIfElseArgsMatch args "r" bar subOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

        Assert.True(isTypeArgsMatch targs "Result, Unit", "ApplyIfElse did not have the correct type arguments")

    [<Fact>]
    [<Trait("Category", "Inequality Condition")>]
    member this.``Inequality with ApplyIfOne``() =
        let result = compileClassicalControlTest 34

        let originalOp =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        [ (1, BuiltIn.ApplyIfOne.FullName) ] |> assertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category", "Inequality Condition")>]
    member this.``Inequality with ApplyIfZero``() =
        let result = compileClassicalControlTest 35

        let originalOp =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        [ (1, BuiltIn.ApplyIfZero.FullName) ] |> assertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Literal on the Left``() =
        let result = compileClassicalControlTest 36

        let originalOp =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        [ (1, BuiltIn.ApplyIfZero.FullName) ] |> assertSpecializationHasCalls originalOp

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Simple NOT condition``() =
        let (_, args) = compileClassicalControlTest 37 |> applyIfElseTest

        isApplyIfElseArgsMatch args "r" subOp2 subOp1
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Outer NOT condition``() =
        let result = compileClassicalControlTest 38
        let ifOp = subOp1
        let elseOp = subOp2

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"

        isApplyIfElseArgsMatch args "r" elseOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[0]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        isApplyIfElseArgsMatch args "r" ifOp elseOp |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``Nested NOT condition``() =
        let result = compileClassicalControlTest 39
        let ifOp = subOp1
        let elseOp = subOp2

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(
            2 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[1]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        let errorMsg = "ApplyIfElse did not have the correct arguments"

        isApplyIfElseArgsMatch args "r" ifOp generated.Parent
        |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

        let lines = generated |> TestUtils.getLinesFromSpecialization

        Assert.True(
            1 = Seq.length lines,
            sprintf "Callable %O(%A) did not have the expected number of statements" generated.Parent generated.Kind
        )

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[0]

        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" generated.Parent generated.Kind)

        isApplyIfElseArgsMatch args "r" ifOp elseOp |> (fun (x, _, _, _, _) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category", "Condition API Conversion")>]
    member this.``One-sided NOT condition``() =
        let (_, args) = compileClassicalControlTest 40 |> applyIfElseTest

        isApplyIfElseArgsMatch args "r" subOp1 noOp
        |> (fun (x, _, _, _, _) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Don't Lift Classical Conditions``() =
        compileClassicalControlTest 41 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Nesting Lift Both``() =
        let result = compileClassicalControlTest 42

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated = TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"

        Assert.True(2 = Seq.length generated) // Should already be asserted by the signature check

        let innerContentCheck call =
            let lines = call |> TestUtils.getBodyFromCallable |> TestUtils.getLinesFromSpecialization
            (List.ofArray lines) = [ "mutable x = 0;"; "set x = 1;" ]

        let (inner, outer) =
            match innerContentCheck (Seq.head generated) with
            | true -> (Seq.head generated, Seq.item 1 generated)
            | false -> (Seq.item 1 generated, Seq.head generated)

        Assert.True(innerContentCheck inner)

        // Make sure original calls outer generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[1]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "r" outer.FullName
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        // Make sure outer calls inner generated
        let lines = outer |> TestUtils.getBodyFromCallable |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfOne.FullName lines.[0]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" outer.FullName QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "r" inner.FullName
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Nesting Lift Outer``() =
        let result = compileClassicalControlTest 43

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[1]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "r" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Nesting Lift Neither``() =
        compileClassicalControlTest 44 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Lift Inner``() =
        let result = compileClassicalControlTest 45

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[4]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        // Make sure the classical condition is present
        Assert.True(lines.[3] = "    if x < 1 {", "The classical condition is missing after transformation")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Lift Outer``() =
        let result = compileClassicalControlTest 46

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[3]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        // Make sure the classical condition is present
        let lines = generated |> TestUtils.getLinesFromSpecialization
        Assert.True(lines.[1] = "if x < 1 {", "The classical condition is missing after transformation")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Lift Outer With More Classic``() =
        let result = compileClassicalControlTest 47

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[3]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        // Make sure the classical condition is present
        let lines = generated |> TestUtils.getLinesFromSpecialization
        Assert.True(lines.[1] = "if x < 1 {", "The classical condition is missing after transformation")
        Assert.True(lines.[2] = "    if x < 2 {", "The classical condition is missing after transformation")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Lift Middle``() =
        let result = compileClassicalControlTest 48

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[4]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        // Make sure the classical condition is present
        Assert.True(lines.[3] = "    if x < 1 {", "The classical condition is missing after transformation")
        let lines = generated |> TestUtils.getLinesFromSpecialization
        Assert.True(lines.[1] = "if x < 2 {", "The classical condition is missing after transformation")
        Assert.True(lines.[2] = "    if x < 3 {", "The classical condition is missing after transformation")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Nested Invalid Lifting``() =
        compileClassicalControlTest 49 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Elif``() =
        let result = compileClassicalControlTest 50

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated = TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"

        Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

        let ifBlockContentCheck call =
            let lines = call |> TestUtils.getBodyFromCallable |> TestUtils.getLinesFromSpecialization
            lines.[1] = "if x < 2 {" && lines.[2] = "    if x < 3 {"

        let elifBlockContentCheck call =
            let lines = call |> TestUtils.getBodyFromCallable |> TestUtils.getLinesFromSpecialization
            lines.[1] = "if x < 4 {" && lines.[2] = "    if x < 5 {"

        let elseBlockContentCheck call =
            let lines = call |> TestUtils.getBodyFromCallable |> TestUtils.getLinesFromSpecialization
            lines.[1] = "if Microsoft.Quantum.Testing.General.M(q) == Zero {" && lines.[2] = "    if x < 6 {"

        let ifBlock = Seq.find ifBlockContentCheck generated
        let elifBlock = Seq.find elifBlockContentCheck generated
        let elseBlock = Seq.find elseBlockContentCheck generated

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization

        Assert.True(lines.[3] = "    if x < 1 {", "The classical condition is missing after transformation")

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[4]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" ifBlock.FullName
        Assert.True(success, "ApplyIfZero did not have the correct arguments")
        Assert.True(lines.[6] = "    else {", "The else condition is missing after transformation")

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfElseR.FullName lines.[7]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _, _, _) =
            isApplyIfElseArgsMatch args "Microsoft.Quantum.Testing.General.M(q)" elifBlock.FullName elseBlock.FullName

        Assert.True(success, "ApplyIfElseR did not have the correct arguments")

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Mutables with Classic Nesting Elif Lift First``() =
        let result = compileClassicalControlTest 51

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo"
            |> Seq.exactlyOne
            |> TestUtils.getBodyFromCallable

        let trimWhitespaceFromLines (lines: string []) = lines |> Array.map (fun s -> s.Trim())

        // Make sure original calls generated
        let lines = original |> TestUtils.getLinesFromSpecialization |> trimWhitespaceFromLines

        Assert.True(lines.[3] = "if x < 1 {", "The classical condition is missing after transformation")

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[4]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, _) = isApplyIfArgMatch args "Microsoft.Quantum.Testing.General.M(q)" generated.Parent
        Assert.True(success, "ApplyIfZero did not have the correct arguments")
        Assert.True(lines.[6] = "else {", "The else condition is missing after transformation")

        Assert.True(
            lines.[7] = "if Microsoft.Quantum.Testing.General.M(q) == Zero {",
            "The quantum condition is missing after transformation"
        )

        Assert.True(lines.[8] = "if x < 4 {", "The classical condition is missing after transformation")
        Assert.True(lines.[9] = "if x < 5 {", "The classical condition is missing after transformation")
        Assert.True(lines.[14] = "else {", "The else condition is missing after transformation")

        Assert.True(
            lines.[16] = "if Microsoft.Quantum.Testing.General.M(q) == Zero {",
            "The quantum condition is missing after transformation"
        )

        Assert.True(lines.[17] = "if x < 6 {", "The classical condition is missing after transformation")

        let lines = generated |> TestUtils.getLinesFromSpecialization |> trimWhitespaceFromLines
        Assert.True(lines.[1] = "if x < 2 {", "The classical condition is missing after transformation")
        Assert.True(lines.[2] = "if x < 3 {", "The classical condition is missing after transformation")

    [<Fact>]
    [<Trait("Category", "If Structure Reshape")>]
    member this.``NOT Condition Retains Used Variables``() =
        compileClassicalControlTest 52 |> ignore

    [<Fact>]
    [<Trait("Category", "Content Lifting")>]
    member this.``Minimal Parameter Capture``() =
        let result = compileClassicalControlTest 53

        let original =
            TestUtils.getCallableWithName result Signatures.ClassicalControlNS "Foo"
            |> TestUtils.getBodyFromCallable

        let generated =
            TestUtils.getCallablesWithSuffix result Signatures.ClassicalControlNS "_Foo" |> Seq.exactlyOne

        let lines = original |> TestUtils.getLinesFromSpecialization

        let (success, _, args) = checkIfStringIsCall BuiltIn.ApplyIfZero.FullName lines.[6]

        Assert.True(
            success,
            sprintf "Callable %O(%A) did not have expected content" original.Parent QsSpecializationKind.QsBody
        )

        let (success, _, args) = isApplyIfArgMatch args "r" generated.FullName
        Assert.True(success, "ApplyIfZero did not have the correct arguments")

        Assert.True(
            (args = "myInt, myDouble, myString, myMutable"),
            "Generated operation did not have the correct arguments"
        )

        let parameters =
            generated.ArgumentTuple.Items
            |> Seq.choose (fun x ->
                match x.VariableName with
                | ValidName str -> Some str
                | InvalidName -> None)
            |> (fun s -> String.Join(", ", s))

        Assert.True(
            (parameters = "myInt, myDouble, myString, myMutable"),
            "Generated operation did not have the correct parameters"
        )
