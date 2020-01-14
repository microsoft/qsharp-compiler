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


    static member private GetLinesFromCallable callable =
        let writer = new SyntaxTreeToQs()

        callable.Specializations
        |> Seq.find (fun x -> x.Kind = QsSpecializationKind.QsBody)
        |> fun x -> match x.Implementation with | Provided (_, body) -> Some body | _ -> None
        |> Option.get
        |> writer.Scope.Transform
        |> ignore

        (writer.Scope :?> ScopeToQs).Output.Split("\r\n")

    static member private CheckIfLineIsCall ``namespace`` name input =
        let call = "(" + (Regex.Escape ``namespace``) + @"\.)?" + (Regex.Escape name) 
        let typeArgs = @"<\s*(.*[^\s])\s*>"
        let args = @"\(\s*(.*[^\s])?\s*\)"
        let regex = "^" + call + @"\s*(" + typeArgs + @")?\s*" + args + @";$"

        let regexMatch = Regex.Match(input, regex)
        if regexMatch.Success then
            (true, regexMatch.Groups.[3].Value, regexMatch.Groups.[4].Value)
        else
            (false, "", "")

    static member private CheckIfCallHasContent call (content : seq<int * string * string>) =
        let lines = LinkingTests.GetLinesFromCallable call
        Seq.forall (fun (i, ns, name) -> LinkingTests.CheckIfLineIsCall ns name lines.[i] |> (fun (x,_,_) -> x)) content

    static member private AssertCallHasContent call content =
        Assert.True(LinkingTests.CheckIfCallHasContent call content, sprintf "Callable %O did not have expected content" call.FullName)

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Basic Hoist`` () =
        let testNumber = 1
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = result.Namespaces
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions
                        |> Seq.find (fun x -> x.Key.Name.Value.EndsWith "_Foo")

        [
            (0, "SubOps", "SubOp1");
            (1, "SubOps", "SubOp2");
            (2, "SubOps", "SubOp3");
        ]
        |> LinkingTests.AssertCallHasContent generated.Value

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Single Expression`` () =
        // Single expressions should not be hoisted into their own operation
        let testNumber = 2
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Use ApplyIfZero And ApplyIfOne`` () =
        let testNumber = 3
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let originalOp = result.Namespaces
                         |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                         |> GlobalCallableResolutions
                         |> Seq.find (fun x -> x.Key.Name.Value = "Foo")

        let getNameFromBuiltin (builtIn : BuiltIn) = builtIn.Namespace.Value, builtIn.Name.Value
        
        [
            (1, getNameFromBuiltin BuiltIn.ApplyIfZero);
            (3, getNameFromBuiltin BuiltIn.ApplyIfOne);
        ]
        |> Seq.map (fun (i,(ns,name)) -> (i,ns,name))
        |> LinkingTests.AssertCallHasContent originalOp.Value


    member private this.ApplyIfElseTest testNumber =
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = result.Namespaces
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions
                        |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_Foo")

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
            if LinkingTests.CheckIfCallHasContent gen1 ifContent then
                LinkingTests.AssertCallHasContent gen2 elseContent
                (gen1, gen2)
            else
                LinkingTests.AssertCallHasContent gen2 ifContent
                LinkingTests.AssertCallHasContent gen1 elseContent
                (gen2, gen1)

        let ifOp, elseOp = getGeneratedCallables (Seq.item 0 generated).Value (Seq.item 1 generated).Value

        let lines = result.Namespaces
                    |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                    |> GlobalCallableResolutions
                    |> Seq.find (fun x -> x.Key.Name.Value = "Foo")
                    |> fun x -> x.Value
                    |> LinkingTests.GetLinesFromCallable

        Assert.True(2 = Seq.length lines, sprintf "Callable %s.%s did not have the expected number of statements" Signatures.ClassicalControlNs "Foo")

        let (success, _, args) = LinkingTests.CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]                          
        Assert.True(success, sprintf "Callable %s.%s did not have expected content" Signatures.ClassicalControlNs "Foo")

        let isApplyIfElseArgsMatch resultVar (opName1 : QsQualifiedName) (opName2 : QsQualifiedName) =

            let op1 = Regex.Escape <| opName1.ToString()
            let op2 = Regex.Escape <| opName2.ToString()

            let ApplyIfElseRegex = sprintf
                                   <| @"^%s, \(%s, \(.*\)\), \(%s, \(.*\)\)$"
                                   <| Regex.Escape resultVar
                                   <| op1
                                   <| op2

            Regex.Match(args, ApplyIfElseRegex).Success

        isApplyIfElseArgsMatch, ifOp.FullName, elseOp.FullName


    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Apply If Zero Else One`` () =
        let (isMatch, ifOp, elseOp) = this.ApplyIfElseTest 4
        Assert.True(isMatch "r" ifOp elseOp, "ApplyIfElse did not have the correct arguments")

    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control Apply If One Else Zero`` () =
        let (isMatch, ifOp, elseOp) = this.ApplyIfElseTest 5
        // The operation arguments should be swapped from the previous test
        Assert.True(isMatch "r" elseOp ifOp, "ApplyIfElse did not have the correct arguments")
    
    [<Fact>]
    [<Trait("Category","Classical Control")>]
    member this.``Classical Control If Elif`` () =
        let testNumber = 6
        let result = this.CompileClassicalControlTest testNumber
        Signatures.SignatureCheck [Signatures.ClassicalControlNs] Signatures.ClassicalControlSignatures.[testNumber-1] result

        let generated = result.Namespaces
                        |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                        |> GlobalCallableResolutions
                        |> Seq.filter (fun x -> x.Key.Name.Value.EndsWith "_Foo")
                        |> Seq.map (fun x -> x.Value)

        Assert.True(3 = Seq.length generated) // Should already be asserted by the signature check

        let ifContent =
            [
                (0, "SubOps", "SubOp1");
                (1, "SubOps", "SubOp2");
            ]
        let ifOp = Seq.tryFind (fun callable -> LinkingTests.CheckIfCallHasContent callable ifContent) generated
                   |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the if block"); callOpion.Value)

        let elifContent =
            [
                (0, "SubOps", "SubOp3");
                (1, "SubOps", "SubOp1");
            ]
        let elifOp = Seq.tryFind (fun callable -> LinkingTests.CheckIfCallHasContent callable elifContent) generated
                     |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the elif block"); callOpion.Value)

        let elseContent =
            [
                (0, "SubOps", "SubOp2");
                (1, "SubOps", "SubOp3");
            ]
        let elseOp = Seq.tryFind (fun callable -> LinkingTests.CheckIfCallHasContent callable elseContent) generated
                     |> (fun callOpion -> Assert.True(callOpion <> None, "Could not find the generated operation for the else block"); callOpion.Value)

        let lines = result.Namespaces
                    |> Seq.filter (fun x -> x.Name.Value = Signatures.ClassicalControlNs)
                    |> GlobalCallableResolutions
                    |> Seq.find (fun x -> x.Key.Name.Value = "Foo")
                    |> fun x -> x.Value
                    |> LinkingTests.GetLinesFromCallable

        Assert.True(2 = Seq.length lines, sprintf "Callable %s.%s did not have the expected number of statements" Signatures.ClassicalControlNs "Foo")

        let (success, _, args) = LinkingTests.CheckIfLineIsCall BuiltIn.ApplyIfElseR.Namespace.Value BuiltIn.ApplyIfElseR.Name.Value lines.[1]                          
        Assert.True(success, sprintf "Callable %s.%s did not have expected content" Signatures.ClassicalControlNs "Foo")

        let isApplyIfElseArgsMatch resultVar (opName1 : QsQualifiedName) (opName2 : QsQualifiedName) =

            let op1 = Regex.Escape <| opName1.ToString()
            let op2 = Regex.Escape <| opName2.ToString()

            let ApplyIfElseRegex = sprintf
                                   <| @"^%s, \(%s, \(.*\)\), \(%s, \(.*\)\)$"
                                   <| Regex.Escape resultVar
                                   <| op1
                                   <| op2

            Regex.Match(args, ApplyIfElseRegex).Success
            
        Assert.True(isApplyIfElseArgsMatch "r" ifOp.FullName { Namespace = BuiltIn.ApplyIfElseR.Namespace; Name = BuiltIn.ApplyIfElseR.Name }, "ApplyIfElse did not have the correct arguments")


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

