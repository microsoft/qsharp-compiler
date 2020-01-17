// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.ClassicallyControlledTransformation
open Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolutionTransformation
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
open Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidation
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler.SyntaxTokens


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests" )) ["Core.qs"; "InvalidEntryPoints.qs"], output)

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    do  let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    static member private ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine ("TestCases", "LinkingTests", fileName) |> File.ReadAllText
        sourceInput.Split ([|"==="|], StringSplitOptions.RemoveEmptyEntries)

    member private this.Expect name (diag : IEnumerable<DiagnosticItem>) =
        let ns = "Microsoft.Quantum.Testing.EntryPoints" |> NonNullable<_>.New
        let name = name |> NonNullable<_>.New
        this.Verify (QsQualifiedName.New (ns, name), diag)

    member private this.CompileAndVerify input (diag : DiagnosticItem seq) =

        let fileId = getTempFile()
        let file = getManager fileId input
        let inFile (c : QsCallable) = c.SourceFile = file.FileName

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let built = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore
        let tests = new CompilerTests(built, output)

        for callable in built.Callables.Values |> Seq.filter inFile do
            tests.Verify (callable.FullName, diag)

    member private this.BuildContent content =
        
        let fileId = getTempFile()
        let file = getManager fileId content

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False
        Assert.NotNull compilationDataStructures.BuiltCompilation

        compilationDataStructures

    member private this.CompileMonomorphization input =

        let compilationDataStructures = this.BuildContent input

        let monomorphicCompilation = MonomorphizationTransformation.Apply compilationDataStructures.BuiltCompilation

        Assert.NotNull monomorphicCompilation
        MonomorphizationValidationTransformation.Apply monomorphicCompilation

        monomorphicCompilation

    member private this.CompileIntrinsicResolution source environment =
        
        let envDS = this.BuildContent environment
        let sourceDS = this.BuildContent source

        IntrinsicResolutionTransformation.Apply(envDS.BuiltCompilation, sourceDS.BuiltCompilation)

    member private this.RunIntrinsicResolutionTest testNumber =
        
        let srcChunks = LinkingTests.ReadAndChunkSourceFile "IntrinsicResolution.qs"
        srcChunks.Length >= 2 * testNumber |> Assert.True
        let chunckNumber = 2 * (testNumber - 1)
        let result = this.CompileIntrinsicResolution srcChunks.[chunckNumber] srcChunks.[chunckNumber+1]
        Signatures.SignatureCheck [Signatures.IntrinsicResolutionNs] Signatures.IntrinsicResolutionSignatures.[testNumber-1] result

        (*Find the overridden operation in the appropriate namespace*)
        let targetCallName = QsQualifiedName.New(NonNullable<_>.New Signatures.IntrinsicResolutionNs, NonNullable<_>.New "Override")
        let targetCallable =
            result.Namespaces
            |> Seq.find (fun ns -> ns.Name.Value = Signatures.IntrinsicResolutionNs)
            |> (fun x -> [x]) |> SyntaxExtensions.Callables
            |> Seq.find (fun call -> call.FullName = targetCallName)

        (*Check that the operation is not intrinsic*)
        targetCallable.Specializations.Length > 0 |> Assert.True
        targetCallable.Specializations
        |> Seq.map (fun spec ->
            match spec.Implementation with
            | Provided _ -> true
            | _ -> false
            |> Assert.True)
        |> ignore

    member private this.CompileClassicalControlTest testNumber =
        let srcChunks = LinkingTests.ReadAndChunkSourceFile "ClassicalControl.qs"
        srcChunks.Length >= testNumber + 1 |> Assert.True
        let shared = srcChunks.[0]
        let compilationDataStructures = this.BuildContent <| shared + srcChunks.[testNumber]
        let processedCompilation = ClassicallyControlledTransformation.Apply compilationDataStructures.BuiltCompilation
        Assert.NotNull processedCompilation
        processedCompilation

    //member private this.RunClassicalControlTest testNumber =
    //    let srcChunks = LinkingTests.ReadAndChunkSourceFile "ClassicalControl.qs"
    //    srcChunks.Length >= testNumber + 1 |> Assert.True
    //    let shared = srcChunks.[0]
    //    let result = this.CompileClassicalControl <| shared + srcChunks.[testNumber]
    //    Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result



    [<Fact>]
    member this.``Monomorphization`` () =
        
        let filePath = Path.Combine ("TestCases", "LinkingTests", "Generics.qs") |> Path.GetFullPath
        let fileId = (new Uri(filePath))
        getManager fileId (File.ReadAllText filePath)
        |> compilationManager.AddOrUpdateSourceFileAsync |> ignore

        for testCase in LinkingTests.ReadAndChunkSourceFile "Monomorphization.qs" |> Seq.zip Signatures.MonomorphizationSignatures do
            this.CompileMonomorphization (snd testCase) |>
            Signatures.SignatureCheck [Signatures.GenericsNs; Signatures.MonomorphizationNs] (fst testCase)

        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution Basic Implementation`` () =
        this.RunIntrinsicResolutionTest 1


    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution Returns UDT`` () =
        this.RunIntrinsicResolutionTest 2

    
    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution Type Mismatch Error`` () =
        Assert.Throws<Exception> (fun _ -> this.RunIntrinsicResolutionTest 3) |> ignore


    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution Param UDT`` () =
        this.RunIntrinsicResolutionTest 4


    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution With Adj`` () =
        this.RunIntrinsicResolutionTest 5


    [<Fact>]
    [<Trait("Category","Intrinsic Resolution")>]
    member this.``Intrinsic Resolution Spec Mismatch Error`` () =
        Assert.Throws<Exception> (fun _ -> this.RunIntrinsicResolutionTest 6) |> ignore


    static member private GetLinesFromSpecialization specialization =
        let writer = new SyntaxTreeToQs()

        specialization
        |> fun x -> match x.Implementation with | Provided (_, body) -> Some body | _ -> None
        |> Option.get
        |> writer.Scope.Transform
        |> ignore

        (writer.Scope :?> ScopeToQs).Output.Split("\r\n")

    static member private CheckIfLineIsCall ``namespace`` name input =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape ``namespace`` <| Regex.Escape name
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?" // Does not support nested type args
        let args = @"\(\s*(.*[^\s])?\s*\)"
        let regex = sprintf @"^%s\s*%s\s*%s;$" call typeArgs args

        let regexMatch = Regex.Match(input, regex)
        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    static member private MakeApplicationRegex (opName : QsQualifiedName) =
        let call = sprintf @"(%s\.)?%s" <| Regex.Escape opName.Namespace.Value <| Regex.Escape opName.Name.Value
        let typeArgs = @"(<\s*([^<]*[^<\s])\s*>)?"  // Does not support nested type args
        let args = @"\(\s*(.*[^\s])?\s*\)"

        sprintf @"\(%s\s*%s,\s*%s\)" <| call <| typeArgs <| args

    static member private isApplyIfArgMatch input resultVar (opName : QsQualifiedName) =
        let regexMatch = Regex.Match(input, sprintf @"^%s,\s*%s$" <| Regex.Escape resultVar <| LinkingTests.MakeApplicationRegex opName)

        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")


    static member private isApplyIfElseArgsMatch input resultVar (opName1 : QsQualifiedName) (opName2 : QsQualifiedName) =
        let ApplyIfElseRegex = sprintf @"^%s,\s*%s,\s*%s$"
                                <| Regex.Escape resultVar
                                <| LinkingTests.MakeApplicationRegex opName1
                                <| LinkingTests.MakeApplicationRegex opName2

        let regexMatch = Regex.Match(input, ApplyIfElseRegex)
        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value, regexMatch.Groups.[7].Value, regexMatch.Groups.[8].Value)
        else
            (false, "", "", "", "")

    static member private CheckIfSpecializationHasContent specialization (content : seq<int * string * string>) =
        let lines = LinkingTests.GetLinesFromSpecialization specialization
        Seq.forall (fun (i, ns, name) -> LinkingTests.CheckIfLineIsCall ns name lines.[i] |> (fun (x,_,_) -> x)) content

    static member private AssertSpecializationHasContent specialization content =
        Assert.True(LinkingTests.CheckIfSpecializationHasContent specialization content, sprintf "Callable %O(%A) did not have expected content" specialization.Parent specialization.Kind)

    static member private GetCallablesWithSuffix compilation ns (suffix : string) =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name.Value = ns)
        |> GlobalCallableResolutions
        |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith suffix)
        |> Seq.map (fun x -> x.Value)

    static member private GetCallableWithName compilation ns name =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name.Value = ns)
        |> GlobalCallableResolutions
        |> Seq.find (fun x -> x.Key.Name.Value = name)
        |> (fun x -> x.Value)

    static member private GetBodyFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)
    static member private GetAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)
    static member private GetCtlFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlled)
    static member private GetCtlAdjFromCallable call = call.Specializations |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsControlledAdjoint)

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Basic Hoist`` () =
        let testNumber = 1
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = LinkingTests.GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x |> LinkingTests.GetBodyFromCallable)

        [
            (0, "SubOps", "SubOp1");
            (1, "SubOps", "SubOp2");
            (2, "SubOps", "SubOp3");
        ]
        |> LinkingTests.AssertSpecializationHasContent generated

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Hoist Loops`` () =
        let testNumber = 2
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Don't Hoist Single Call`` () =
        // Single calls should not be hoisted into their own operation
        let testNumber = 3
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Hoist Single Non-Call`` () =
        // Single expressions that are not calls should be hoisted into their own operation
        let testNumber = 4
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Don't Hoist Return Statments`` () =
        let testNumber = 5
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control All-Or-None Hoisting`` () =
        let testNumber = 6
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control ApplyIfZero And ApplyIfOne`` () =
        let testNumber = 7
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let originalOp = LinkingTests.GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> LinkingTests.GetBodyFromCallable

        let getNameFromBuiltin (builtIn : BuiltIn) = builtIn.Namespace.Value, builtIn.Name.Value
        
        [
            (1, getNameFromBuiltin BuiltIn.ApplyIfZero);
            (3, getNameFromBuiltin BuiltIn.ApplyIfOne);
        ]
        |> Seq.map (fun (i,(ns,name)) -> (i,ns,name))
        |> LinkingTests.AssertSpecializationHasContent originalOp


    member private this.ApplyIfElseTest testNumber =
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = LinkingTests.GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo" |> Seq.map LinkingTests.GetBodyFromCallable

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

        let getGeneratedCallables gen1 gen2 =
            if LinkingTests.CheckIfSpecializationHasContent gen1 ifContent then
                LinkingTests.AssertSpecializationHasContent gen2 elseContent
                (gen1, gen2)
            else
                LinkingTests.AssertSpecializationHasContent gen2 ifContent
                LinkingTests.AssertSpecializationHasContent gen1 elseContent
                (gen2, gen1)

        let ifOp, elseOp = getGeneratedCallables (Seq.item 0 generated) (Seq.item 1 generated)

        let original = LinkingTests.GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> LinkingTests.GetBodyFromCallable
        let lines = original |> LinkingTests.GetLinesFromSpecialization

        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, _, args) = LinkingTests.CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]                          
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)

        args, ifOp.Parent, elseOp.Parent


    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Apply If Zero Else One`` () =
        let (args, ifOp, elseOp) = this.ApplyIfElseTest 8
        LinkingTests.isApplyIfElseArgsMatch args "r" ifOp elseOp
        |> (fun (x,_,_,_,_) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Apply If One Else Zero`` () =
        let (args, ifOp, elseOp) = this.ApplyIfElseTest 9
        // The operation arguments should be swapped from the previous test
        LinkingTests.isApplyIfElseArgsMatch args "r" elseOp ifOp
        |> (fun (x,_,_,_,_) -> Assert.True(x, "ApplyIfElse did not have the correct arguments"))
    
    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control If Elif`` () =
        let testNumber = 10
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = LinkingTests.GetCallablesWithSuffix result Signatures.ClassicalControlNs "_Foo" |> Seq.map LinkingTests.GetBodyFromCallable

        Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

        let ifContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let ifOp = Seq.tryFind (fun spec -> LinkingTests.CheckIfSpecializationHasContent spec ifContent) generated
                   |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the if block"); callOpion.Value)

        let elifContent =
            [
                (0, "SubOps", "SubOp3");
                (1, "SubOps", "SubOp1");
            ]
        let elifOp = Seq.tryFind (fun spec -> LinkingTests.CheckIfSpecializationHasContent spec elifContent) generated
                     |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the elif block"); callOpion.Value)

        let elseContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
            ]
        let elseOp = Seq.tryFind (fun spec -> LinkingTests.CheckIfSpecializationHasContent spec elseContent) generated
                     |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the else block"); callOpion.Value)

        let original = LinkingTests.GetCallableWithName result Signatures.ClassicalControlNs "Foo" |> LinkingTests.GetBodyFromCallable
        let lines = original |> LinkingTests.GetLinesFromSpecialization

        Assert.True(2 = Seq.length lines, sprintf "Callable %O(%A) did not have the expected number of statements" original.Parent original.Kind)

        let (success, _, args) = LinkingTests.CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]                          
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.Parent original.Kind)
         
        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, _, _, subArgs) = LinkingTests.isApplyIfElseArgsMatch args "r" ifOp.Parent { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name }
        Assert.True(success, errorMsg)
        LinkingTests.isApplyIfElseArgsMatch subArgs "r" elifOp.Parent elseOp.Parent
        |> (fun (x,_,_,_,_) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control And Condition`` () =
        let (args, ifOp, elseOp) = this.ApplyIfElseTest 11
        
        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, subArgs, _, _) = LinkingTests.isApplyIfElseArgsMatch args "r" { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name } elseOp
        Assert.True(success, errorMsg)
        LinkingTests.isApplyIfElseArgsMatch subArgs "r" ifOp elseOp
        |> (fun (x,_,_,_,_) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Or Condition`` () =
        let (args, ifOp, elseOp) = this.ApplyIfElseTest 12
        
        let errorMsg = "ApplyIfElse did not have the correct arguments"
        let (success, _, _, _, subArgs) = LinkingTests.isApplyIfElseArgsMatch args "r" ifOp { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name }
        Assert.True(success, errorMsg)
        LinkingTests.isApplyIfElseArgsMatch subArgs "r" ifOp elseOp
        |> (fun (x,_,_,_,_) -> Assert.True(x, errorMsg))

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Don't Hoist Functions`` () =
        let testNumber = 13
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Hoist Self-Contained Mutable`` () =
        let testNumber = 14
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Don't Hoist General Mutable`` () =
        let testNumber = 15
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Generics Support`` () =
        let testNumber = 16
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

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

        (*Assert that the generated operation has the same type parameters as the original operation*)
        let originalTypeParams = GetTypeParams original
        let generatedTypeParams = GetTypeParams generated
        AssertTypeArgsMatch originalTypeParams generatedTypeParams

        (*Assert that the original operation calls the generated operation with the appropriate type arguments*)
        let lines = LinkingTests.GetBodyFromCallable original |> LinkingTests.GetLinesFromSpecialization
        let (success, _, args) = LinkingTests.CheckIfLineIsCall BuiltIn.ApplyIfZero.Namespace.Value BuiltIn.ApplyIfZero.Name.Value lines.[1]                          
        Assert.True(success, sprintf "Callable %O(%A) did not have expected content" original.FullName QsSpecializationKind.QsBody)

        let (success, typeArgs, _) = LinkingTests.isApplyIfArgMatch args "r" generated.FullName
        Assert.True(success, sprintf "ApplyIfZero did not have the correct arguments")

        AssertTypeArgsMatch originalTypeParams <| typeArgs.Replace("'", "").Replace(" ", "").Split(",")

    static member private DoesCallSupportsFunctors expectedFunctors call =
        let hasAdjoint = expectedFunctors |> Seq.contains QsFunctor.Adjoint
        let hasControlled = expectedFunctors |> Seq.contains QsFunctor.Controlled
        
        (*Checks the Characteristics match*)
        let charMatch = lazy match call.Signature.Information.Characteristics.SupportedFunctors with
                             | Value x -> x.SetEquals(expectedFunctors)
                             | Null -> 0 = Seq.length expectedFunctors
        
        (*Checks that the target specializations are present*)
        let adjMatch = lazy if hasAdjoint then
                                call.Specializations
                                |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsAdjoint)
                                |> function
                                   | None -> false
                                   | Some x -> match x.Implementation with
                                               | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Invert
                                               | _ -> false
                            else true
            
        let ctlMatch = lazy if hasControlled then
                                call.Specializations
                                |> Seq.tryFind (fun x -> x.Kind = QsSpecializationKind.QsControlled)
                                |> function
                                   | None -> false
                                   | Some x -> match x.Implementation with
                                               | SpecializationImplementation.Generated gen -> gen = QsGeneratorDirective.Distribute
                                               | _ -> false
                            else true
        
        charMatch.Value && adjMatch.Value && ctlMatch.Value

    static member private AssertCallSupportsFunctors expectedFunctors call =
        Assert.True(LinkingTests.DoesCallSupportsFunctors expectedFunctors call, sprintf "Callable %O did not support the expected functors" call.FullName)

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Adjoint Support`` () =
        let testNumber = 17
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

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

        let getNameFromBuiltin (builtIn : BuiltIn) = builtIn.Namespace.Value, builtIn.Name.Value
        
        [(1, getNameFromBuiltin BuiltIn.ApplyIfZero)]
        |> Seq.map (fun (i,(ns,name)) -> (i,ns,name))
        |> LinkingTests.AssertSpecializationHasContent (LinkingTests.GetBodyFromCallable selfOp)

        [(1, getNameFromBuiltin BuiltIn.ApplyIfZeroA)]
        |> Seq.map (fun (i,(ns,name)) -> (i,ns,name))
        |> LinkingTests.AssertSpecializationHasContent (LinkingTests.GetBodyFromCallable invertOp)

        [(1, getNameFromBuiltin BuiltIn.ApplyIfZero)]
        |> Seq.map (fun (i,(ns,name)) -> (i,ns,name))
        |> LinkingTests.AssertSpecializationHasContent (LinkingTests.GetBodyFromCallable selfOp)

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

        let getGeneratedCallables gen1 gen2 =
            let spec1 = LinkingTests.GetBodyFromCallable gen1
            let spec2 = LinkingTests.GetBodyFromCallable gen2
            if LinkingTests.CheckIfSpecializationHasContent spec1 bodyContent then
                LinkingTests.AssertSpecializationHasContent spec2 adjointContent
                (gen1, gen2)
            else
                LinkingTests.AssertSpecializationHasContent spec2 bodyContent
                LinkingTests.AssertSpecializationHasContent spec1 adjointContent
                (gen2, gen1)

        let bodyProvidedOp, adjointProvidedOp = getGeneratedCallables (Seq.item 0 _providedOps) (Seq.item 1 _providedOps)

        LinkingTests.AssertCallSupportsFunctors [] _selfOp
        LinkingTests.AssertCallSupportsFunctors [QsFunctor.Adjoint] _invertOp
        LinkingTests.AssertCallSupportsFunctors [] bodyProvidedOp
        LinkingTests.AssertCallSupportsFunctors [] adjointProvidedOp


    [<Fact>]
    member this.``Fail on multiple entry points`` () =

        let entryPoints = LinkingTests.ReadAndChunkSourceFile "ValidEntryPoints.qs"
        Assert.True (entryPoints.Length > 1)

        let fileId = getTempFile()
        let file = getManager fileId entryPoints.[0]
        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        this.CompileAndVerify entryPoints.[1] [Error ErrorCode.MultipleEntryPoints]
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore


    [<Fact>]
    member this.``Entry point specialization verification`` () =

        for entryPoint in LinkingTests.ReadAndChunkSourceFile "EntryPointSpecializations.qs" do
            this.CompileAndVerify entryPoint [Error ErrorCode.InvalidEntryPointSpecialization]

        for entryPoint in LinkingTests.ReadAndChunkSourceFile "ValidEntryPoints.qs" do
            this.CompileAndVerify entryPoint []


    [<Fact>]
    member this.``Entry point attribute placement verification`` () =

        this.Expect "InvalidEntryPointPlacement1" [Error ErrorCode.InvalidEntryPointPlacement]
        this.Expect "InvalidEntryPointPlacement2" [Error ErrorCode.InvalidEntryPointPlacement]
        this.Expect "InvalidEntryPointPlacement3" [Error ErrorCode.InvalidEntryPointPlacement]

        // the error messages here should become InvalidEntryPointPlacement if / when
        // we support attaching attributes to specializations in general
        this.Expect "InvalidEntryPointPlacement4" [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidEntryPointPlacement5" [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidEntryPointPlacement6" [Error ErrorCode.MisplacedDeclarationAttribute]
        this.Expect "InvalidEntryPointPlacement7" [Error ErrorCode.MisplacedDeclarationAttribute]


    [<Fact>]
    member this.``Entry point argument and return type verification`` () =

        this.Expect "InvalidEntryPoint1"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint2"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint3"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint4"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint5"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint6"  [Error ErrorCode.QubitTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint7"  [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint8"  [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint9"  [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint10" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint11" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint12" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint13" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint14" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint15" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint16" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint17" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint18" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint19" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint20" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint21" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint22" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint23" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint24" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint25" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint26" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.QubitTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint27" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint28" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint29" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint30" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint31" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint32" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint33" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint34" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint35" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint36" [Error ErrorCode.CallableTypeInEntryPointSignature; Error ErrorCode.UserDefinedTypeInEntryPointSignature]

