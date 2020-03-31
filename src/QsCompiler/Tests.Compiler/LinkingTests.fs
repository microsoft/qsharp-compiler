// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
open Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests" )) ["Core.qs"; "InvalidEntryPoints.qs"] [], output)

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    // The file name needs to end in ".qs" so that it isn't ignored by the References.Headers class during the internal renaming tests.
    let getTempFile () = Path.GetRandomFileName () + ".qs" |> Path.GetFullPath |> Uri
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let defaultOffset = {
        Offset = DiagnosticTools.AsTuple (Position (0, 0))
        Range = QsCompilerDiagnostic.DefaultRange
    }

    let qualifiedName ns name = {
            Namespace = NonNullable<_>.New ns
            Name = NonNullable<_>.New name
        }

    let createReferences : seq<string * IEnumerable<QsNamespace>> -> References =
        Seq.map (fun (source, namespaces) ->
            KeyValuePair.Create(NonNullable<_>.New source, References.Headers (NonNullable<_>.New source, namespaces)))
        >> ImmutableDictionary.CreateRange
        >> References

    /// Counts the number of references to the qualified name in all of the namespaces, including the declaration.
    let countReferences namespaces (name : QsQualifiedName) =
        let references = IdentifierReferences (name, defaultOffset)
        Seq.iter (references.Namespaces.OnNamespace >> ignore) namespaces
        let declaration = if obj.ReferenceEquals (references.SharedState.DeclarationLocation, null) then 0 else 1
        references.SharedState.Locations.Count + declaration

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

    member private this.BuildContent (source, ?references) =
        let fileId = getTempFile ()
        let file = getManager fileId source

        match references with
        | Some references -> compilationManager.UpdateReferencesAsync references |> ignore
        | None -> ()
        compilationManager.AddOrUpdateSourceFileAsync file |> ignore

        let compilation = compilationManager.Build ()
        compilationManager.TryRemoveSourceFileAsync (fileId, false) |> ignore
        compilationManager.UpdateReferencesAsync (References ImmutableDictionary<_, _>.Empty) |> ignore

        compilation.Diagnostics () |> Seq.exists (fun d -> d.IsError ()) |> Assert.False
        Assert.NotNull compilation.BuiltCompilation

        compilation

    member private this.CompileMonomorphization input =

        let compilationDataStructures = this.BuildContent input

        let monomorphicCompilation = Monomorphize.Apply compilationDataStructures.BuiltCompilation

        Assert.NotNull monomorphicCompilation
        ValidateMonomorphization.Apply monomorphicCompilation

        monomorphicCompilation

    member private this.CompileIntrinsicResolution source environment =

        let envDS = this.BuildContent environment
        let sourceDS = this.BuildContent source

        ReplaceWithTargetIntrinsics.Apply(envDS.BuiltCompilation, sourceDS.BuiltCompilation)

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

    /// Runs the nth internal renaming test, asserting that declarations with the given name and references to them have
    /// been renamed across the compilation unit.
    member private this.RunInternalRenamingTest num renamed notRenamed =
        let chunks = LinkingTests.ReadAndChunkSourceFile "InternalRenaming.qs"
        let sourceCompilation = this.BuildContent chunks.[num - 1]

        let namespaces =
            sourceCompilation.BuiltCompilation.Namespaces
            |> Seq.filter (fun ns -> ns.Name.Value.StartsWith Signatures.InternalRenamingNs)
        let references = createReferences ["InternalRenaming.dll", namespaces]
        let referenceCompilation = this.BuildContent ("", references)

        let countAll namespaces names =
            names |> Seq.map (countReferences namespaces) |> Seq.sum

        let beforeCount = countAll sourceCompilation.BuiltCompilation.Namespaces (Seq.concat [renamed; notRenamed])
        let afterCountOriginal = countAll referenceCompilation.BuiltCompilation.Namespaces renamed

        let newNames = renamed |> Seq.map (fun name -> CompilationUnit.ReferenceDecorator.Decorate (name, 0))
        let afterCount = countAll referenceCompilation.BuiltCompilation.Namespaces (Seq.concat [newNames; notRenamed])

        Assert.NotEqual (0, beforeCount)
        Assert.Equal (0, afterCountOriginal)
        Assert.Equal (beforeCount, afterCount)

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

    [<Fact>]
    member this.``Rename internal operation call references`` () =
        this.RunInternalRenamingTest 1
            [qualifiedName Signatures.InternalRenamingNs "Foo"]
            [qualifiedName Signatures.InternalRenamingNs "Bar"]

    [<Fact>]
    member this.``Rename internal function call references`` () =
        this.RunInternalRenamingTest 2
            [qualifiedName Signatures.InternalRenamingNs "Foo"]
            [qualifiedName Signatures.InternalRenamingNs "Bar"]

    [<Fact>]
    member this.``Rename internal type references`` () =
        this.RunInternalRenamingTest 3
            [
                qualifiedName Signatures.InternalRenamingNs "Foo"
                qualifiedName Signatures.InternalRenamingNs "Bar"
                qualifiedName Signatures.InternalRenamingNs "Baz"
            ]
            []

    [<Fact>]
    member this.``Rename internal references across namespaces`` () =
        this.RunInternalRenamingTest 4
            [
                qualifiedName Signatures.InternalRenamingNs "Foo"
                qualifiedName Signatures.InternalRenamingNs "Bar"
                qualifiedName (Signatures.InternalRenamingNs + ".Extra") "Qux"
            ]
            [qualifiedName (Signatures.InternalRenamingNs + ".Extra") "Baz"]

    [<Fact>]
    member this.``Rename internal qualified references`` () =
        this.RunInternalRenamingTest 5
            [
                qualifiedName Signatures.InternalRenamingNs "Foo"
                qualifiedName Signatures.InternalRenamingNs "Bar"
                qualifiedName (Signatures.InternalRenamingNs + ".Extra") "Qux"
            ]
            [qualifiedName (Signatures.InternalRenamingNs + ".Extra") "Baz"]

    [<Fact>]
    member this.``Rename internal attribute references`` () =
        this.RunInternalRenamingTest 6
            [qualifiedName Signatures.InternalRenamingNs "Foo"]
            [qualifiedName Signatures.InternalRenamingNs "Bar"]

    [<Fact>]
    member this.``Rename specializations for internal operations`` () =
        this.RunInternalRenamingTest 7
            [qualifiedName Signatures.InternalRenamingNs "Foo"]
            [qualifiedName Signatures.InternalRenamingNs "Bar"]

    [<Fact>]
    member this.``Group internal specializations by source file`` () =
        let chunks = LinkingTests.ReadAndChunkSourceFile "InternalRenaming.qs"
        let sourceCompilation = this.BuildContent chunks.[7]
        let namespaces =
            sourceCompilation.BuiltCompilation.Namespaces
            |> Seq.filter (fun ns -> ns.Name.Value.StartsWith Signatures.InternalRenamingNs)

        let references = createReferences ["InternalRenaming1.dll", namespaces
                                           "InternalRenaming2.dll", namespaces]
        let referenceCompilation = this.BuildContent ("", references)
        let callables = GlobalCallableResolutions referenceCompilation.BuiltCompilation.Namespaces

        for i in 0 .. references.Declarations.Count - 1 do
            let name =
                CompilationUnit.ReferenceDecorator.Decorate (qualifiedName Signatures.InternalRenamingNs "Foo", i)
            let specializations = callables.[name].Specializations
            Assert.Equal (4, specializations.Length)
            Assert.True (specializations |> Seq.forall (fun s -> s.SourceFile = callables.[name].SourceFile))
