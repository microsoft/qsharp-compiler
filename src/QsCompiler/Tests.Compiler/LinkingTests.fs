// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.VisualStudio.LanguageServer.Protocol
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
open Xunit
open Xunit.Abstractions


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests" )) ["Core.qs"; "InvalidEntryPoints.qs"] [], output)

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName() + ".qs"))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let defaultOffset = {
        Offset = DiagnosticTools.AsTuple (Position (0, 0))
        Range = QsCompilerDiagnostic.DefaultRange
    }

    /// Counts the number of references to the qualified name in the headers and specialization implementations. The
    /// declaration of the name is included in the count.
    let countReferencesInHeaders (headers : References.Headers) (name : QsQualifiedName) =
        let references = IdentifierReferences (name, defaultOffset)
        references.SharedState.DeclarationOffset <- (0, 0)

        for callable in headers.Callables do
            Seq.iter (references.Namespaces.OnAttribute >> ignore) callable.Attributes
            references.Namespaces.OnArgumentTuple callable.ArgumentTuple |> ignore
            references.Namespaces.OnSignature callable.Signature |> ignore
        for (specialization, implementation) in headers.Specializations do
            Seq.iter (references.Namespaces.OnAttribute >> ignore) specialization.Attributes
            references.Namespaces.OnSpecializationImplementation implementation |> ignore
        for qsType in headers.Types do
            Seq.iter (references.Namespaces.OnAttribute >> ignore) qsType.Attributes
            references.Types.OnType qsType.Type |> ignore
            references.Namespaces.OnTypeItems qsType.TypeItems |> ignore

        let count =
            // TODO: We ignore type constructors because countReferencesInNamespaces is not able to count them.
            headers.Callables.Count(fun callable -> callable.QualifiedName = name && callable.Kind <> TypeConstructor) +
            headers.Types.Count(fun qsType -> qsType.QualifiedName = name) +
            references.SharedState.Locations.Count
        count

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
        let dllSource = "InternalRenaming.dll"
        let newNames = renamed |> Seq.map (dllSource |> FuncConvert.FuncFromTupled References.GetNewNameForInternal)

        let chunks = LinkingTests.ReadAndChunkSourceFile "InternalRenaming.qs"
        let compilation = this.BuildContent chunks.[num - 1]
        let originalHeaders = new References.Headers(NonNullable<string>.New dllSource, compilation.BuiltCompilation.Namespaces)

        let loadedReferences  = 
            [KeyValuePair.Create(NonNullable<_>.New dllSource, originalHeaders)]
            |> ImmutableDictionary.CreateRange
            |> References

        let HeadersFromReference (references : References) = 
            references.Declarations.Single().Value

        let CountReferencesFor itemsToCount headers = 
            itemsToCount 
            |> Seq.map (countReferencesInHeaders headers)
            |> Seq.sum

        let beforeCount =
            originalHeaders
            |> CountReferencesFor (Seq.concat [renamed; notRenamed])

        let afterCount =
            loadedReferences |> HeadersFromReference
            |> CountReferencesFor (Seq.concat [newNames; notRenamed])

        let afterCountOriginalName =
            loadedReferences |> HeadersFromReference
            |> CountReferencesFor renamed

        Assert.NotEqual (0, beforeCount)
        Assert.Equal (0, afterCountOriginalName)
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
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }]
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }]

    [<Fact>]
    member this.``Rename internal function call references`` () =
        this.RunInternalRenamingTest 2
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }]
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }]

    [<Fact>]
    member this.``Rename internal type references`` () =
        this.RunInternalRenamingTest 3
            [
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Baz" }
            ]
            []

    [<Fact>]
    member this.``Rename internal references across namespaces`` () =
        this.RunInternalRenamingTest 4
            [
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }
                { Namespace = NonNullable<_>.New (Signatures.InternalRenamingNs + ".Extra"); Name = NonNullable<_>.New "Qux" }
            ]
            [{ Namespace = NonNullable<_>.New (Signatures.InternalRenamingNs + ".Extra"); Name = NonNullable<_>.New "Baz" }]

    [<Fact>]
    member this.``Rename internal qualified references`` () =
        this.RunInternalRenamingTest 5
            [
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }
                { Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }
                { Namespace = NonNullable<_>.New (Signatures.InternalRenamingNs + ".Extra"); Name = NonNullable<_>.New "Qux" }
            ]
            [{ Namespace = NonNullable<_>.New (Signatures.InternalRenamingNs + ".Extra"); Name = NonNullable<_>.New "Baz" }]

    [<Fact>]
    member this.``Rename internal attribute references`` () =
        this.RunInternalRenamingTest 6
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Foo" }]
            [{ Namespace = NonNullable<_>.New Signatures.InternalRenamingNs; Name = NonNullable<_>.New "Bar" }]
