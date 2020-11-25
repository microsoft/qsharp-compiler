// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations
open Microsoft.Quantum.QsCompiler.Transformations.IntrinsicResolution
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization.Validation
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests"), ["Core.qs"; "InvalidEntryPoints.qs"]))

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message), isExecutable = true)

    // The file name needs to end in ".qs" so that it isn't ignored by the References.Headers class during the internal renaming tests.
    let getTempFile () = Path.GetRandomFileName () + ".qs" |> Path.GetFullPath |> Uri
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    let defaultOffset = {
        Offset = Position.Zero
        Range = Range.Zero
    }

    let qualifiedName ns name = {
            Namespace = ns
            Name = name
        }

    let createReferences : seq<string * IEnumerable<QsNamespace>> -> References =
        Seq.map (fun (source, namespaces) ->
            KeyValuePair.Create(source, References.Headers (source, namespaces)))
        >> ImmutableDictionary.CreateRange
        >> References

    /// Counts the number of references to the qualified name in all of the namespaces, including the declaration.
    let countReferences namespaces (name : QsQualifiedName) =
        let references = IdentifierReferences (name, defaultOffset)
        Seq.iter (references.Namespaces.OnNamespace >> ignore) namespaces
        let declaration = if obj.ReferenceEquals (references.SharedState.DeclarationLocation, null) then 0 else 1
        references.SharedState.Locations.Count + declaration

    let getCallablesWithSuffix compilation ns (suffix : string) =
        compilation.Namespaces
        |> Seq.filter (fun x -> x.Name = ns)
        |> GlobalCallableResolutions
        |> Seq.filter (fun x -> x.Key.Name.EndsWith suffix)
        |> Seq.map (fun x -> x.Value)

    do  let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    static member private ReadAndChunkSourceFile fileName =
        let sourceInput = Path.Combine ("TestCases", "LinkingTests", fileName) |> File.ReadAllText
        sourceInput.Split ([|"==="|], StringSplitOptions.RemoveEmptyEntries)

    member private this.Expect name (diag : IEnumerable<DiagnosticItem>) =
        let ns = "Microsoft.Quantum.Testing.EntryPoints"
        this.VerifyDiagnostics (QsQualifiedName.New (ns, name), diag)

    member private this.BuildWithSource input (manager : CompilationUnitManager) = 
        let fileId = getTempFile()
        let file = getManager fileId input
        manager.AddOrUpdateSourceFileAsync(file) |> ignore
        let built = manager.Build()
        manager.TryRemoveSourceFileAsync(fileId, false) |> ignore
        file.FileName, built

    member private this.CompileAndVerify (manager : CompilationUnitManager) input (diag : DiagnosticItem seq) =
        let source, built = manager |> this.BuildWithSource input
        let tests = new CompilerTests(built)

        let inFile (c : QsCallable) = Source.assemblyOrCode c.Source = source
        for callable in built.Callables.Values |> Seq.filter inFile do
            tests.VerifyDiagnostics (callable.FullName, diag)

    member private this.BuildContent (manager : CompilationUnitManager, source, ?references) =
        match references with
        | Some references -> manager.UpdateReferencesAsync references |> ignore
        | None -> ()
        let _, compilation = manager |> this.BuildWithSource source
        manager.UpdateReferencesAsync (References ImmutableDictionary<_, _>.Empty) |> ignore

        let diagnostics = compilation.Diagnostics()
        diagnostics |> Seq.exists (fun d -> d.IsError ()) |> Assert.False
        Assert.NotNull compilation.BuiltCompilation

        compilation

    member private this.BuildReference (source : string, content) =
        let comp = this.BuildContent(new CompilationUnitManager(), content)
        Assert.Empty (comp.Diagnostics() |> Seq.filter (fun d -> d.Severity = DiagnosticSeverity.Error))
        struct (source, comp.BuiltCompilation.Namespaces)

    member private this.CompileMonomorphization input =
        let compilationDataStructures = this.BuildContent (compilationManager, input)
        let monomorphicCompilation = Monomorphize.Apply compilationDataStructures.BuiltCompilation

        Assert.NotNull monomorphicCompilation
        ValidateMonomorphization.Apply monomorphicCompilation

        monomorphicCompilation

    member private this.CompileIntrinsicResolution source environment =
        let envDS = this.BuildContent (compilationManager, environment)
        let sourceDS = this.BuildContent (compilationManager, source)
        ReplaceWithTargetIntrinsics.Apply(envDS.BuiltCompilation, sourceDS.BuiltCompilation)

    member private this.RunIntrinsicResolutionTest testNumber =

        let srcChunks = LinkingTests.ReadAndChunkSourceFile "IntrinsicResolution.qs"
        srcChunks.Length >= 2 * testNumber |> Assert.True
        let chunckNumber = 2 * (testNumber - 1)
        let result = this.CompileIntrinsicResolution srcChunks.[chunckNumber] srcChunks.[chunckNumber+1]
        Signatures.SignatureCheck [Signatures.IntrinsicResolutionNs] Signatures.IntrinsicResolutionSignatures.[testNumber-1] result

        (*Find the overridden operation in the appropriate namespace*)
        let targetCallName = QsQualifiedName.New(Signatures.IntrinsicResolutionNs, "Override")
        let targetCallable =
            result.Namespaces
            |> Seq.find (fun ns -> ns.Name = Signatures.IntrinsicResolutionNs)
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
        let manager = new CompilationUnitManager()
        let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> manager.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile
        let sourceCompilation = this.BuildContent (manager, chunks.[num - 1])

        let namespaces =
            sourceCompilation.BuiltCompilation.Namespaces
            |> Seq.filter (fun ns -> ns.Name.StartsWith Signatures.InternalRenamingNs)
        let references = createReferences ["InternalRenaming.dll", namespaces]
        let referenceCompilation = this.BuildContent (manager, "", references)

        let countAll namespaces names =
            names |> Seq.map (countReferences namespaces) |> Seq.sum

        let beforeCount = countAll sourceCompilation.BuiltCompilation.Namespaces (Seq.concat [renamed; notRenamed])
        let afterCountOriginal = countAll referenceCompilation.BuiltCompilation.Namespaces renamed

        let decorator = new NameDecorator("QsRef");
        let newNames = renamed |> Seq.map (fun name -> decorator.Decorate (name, 0))
        let afterCount = countAll referenceCompilation.BuiltCompilation.Namespaces (Seq.concat [newNames; notRenamed])

        Assert.NotEqual (0, beforeCount)
        Assert.Equal (0, afterCountOriginal)
        Assert.Equal (beforeCount, afterCount)

    member private this.RunMonomorphizationAccessModifierTest testNumber =
        let source = (LinkingTests.ReadAndChunkSourceFile "Monomorphization.qs").[testNumber]
        let compilation = this.CompileMonomorphization source

        let generated = getCallablesWithSuffix compilation Signatures.MonomorphizationNs "_IsInternalUsesInternal"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x)
        
        Assert.True((match generated.Modifiers.Access with | Internal -> true | _ -> false),
            "Callables originally internal should remain internal.")

        let generated = getCallablesWithSuffix compilation Signatures.MonomorphizationNs "_IsInternalUsesPublic"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x)
        
        Assert.True((match generated.Modifiers.Access with | Internal -> true | _ -> false),
            "Callables originally internal should remain internal.")

        let generated = getCallablesWithSuffix compilation Signatures.MonomorphizationNs "_IsPublicUsesInternal"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x)
        
        Assert.True((match generated.Modifiers.Access with | Internal -> true | _ -> false),
            "Callables with internal arguments should be internal.")

        let generated = getCallablesWithSuffix compilation Signatures.MonomorphizationNs "_IsPublicUsesPublic"
                        |> (fun x -> Assert.True(1 = Seq.length x); Seq.item 0 x)
        
        Assert.True((match generated.Modifiers.Access with | DefaultAccess -> true | _ -> false),
            "Callables originally public should remain public if all arguments are public.")

    
    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Basic Implementation`` () =

        let filePath = Path.Combine ("TestCases", "LinkingTests", "Generics.qs") |> Path.GetFullPath
        let fileId = (new Uri(filePath))
        getManager fileId (File.ReadAllText filePath)
        |> compilationManager.AddOrUpdateSourceFileAsync |> ignore

        for testCase in LinkingTests.ReadAndChunkSourceFile "Monomorphization.qs" |> Seq.zip Signatures.MonomorphizationSignatures do
            this.CompileMonomorphization (snd testCase) |>
            Signatures.SignatureCheck [Signatures.GenericsNs; Signatures.MonomorphizationNs] (fst testCase)

        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Args`` () =
        this.RunMonomorphizationAccessModifierTest 4


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Returns`` () =
        this.RunMonomorphizationAccessModifierTest 5


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Array Args`` () =
        this.RunMonomorphizationAccessModifierTest 6


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Array Returns`` () =
        this.RunMonomorphizationAccessModifierTest 7


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Tuple Args`` () =
        this.RunMonomorphizationAccessModifierTest 8


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Tuple Returns`` () =
        this.RunMonomorphizationAccessModifierTest 9


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Op Args`` () =
        this.RunMonomorphizationAccessModifierTest 10


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Access Modifier Resolution Op Returns`` () =
        this.RunMonomorphizationAccessModifierTest 11


    [<Fact>]
    [<Trait("Category","Monomorphization")>]
    member this.``Monomorphization Type Parameter Resolutions`` () =
        let source = (LinkingTests.ReadAndChunkSourceFile "Monomorphization.qs").[12]
        let compilation = this.CompileMonomorphization source

        let callables = compilation.Namespaces |> GlobalCallableResolutions
        Assert.Contains(BuiltIn.Length.FullName, callables.Keys)
        Assert.Contains(BuiltIn.RangeReverse.FullName, callables.Keys)
        Assert.DoesNotContain(BuiltIn.IndexRange.FullName, callables.Keys)

        let isGlobalCallable tag = function 
            | Identifier (GlobalCallable id, _) -> tag id
            | _ -> false
        let isConcretizationOf (expected : QsQualifiedName) (given : QsQualifiedName) = 
            given.Namespace = expected.Namespace && 
            given.Name.Length > 34 &&
            given.Name.[0] = '_' &&
            given.Name.[33] = '_' &&
            given.Name.[34..] = expected.Name

        let mutable gotLength, gotIndexRange = false, false
        let onExpr (ex : TypedExpression) = 
            match ex.Expression with 
            | CallLikeExpression (lhs, _) -> 
                if lhs.Expression |> isGlobalCallable ((=)BuiltIn.Length.FullName) then
                    gotLength <- true
                    Assert.Equal(1, ex.TypeArguments.Length)
                    let parent, _, resolution = ex.TypeArguments |> Seq.head
                    Assert.Equal(BuiltIn.Length.FullName, parent)
                    Assert.Equal(Int, resolution.Resolution)
                elif lhs.Expression |> isGlobalCallable (isConcretizationOf BuiltIn.IndexRange.FullName) then
                    gotIndexRange <- true
                    Assert.Equal(0, ex.TypeParameterResolutions.Count)
            | _ -> ()

        let walker = TypedExpressionWalker(Action<_> onExpr, ())
        walker.Transformation.OnCompilation compilation |> ignore
        Assert.True(gotLength)
        Assert.True(gotIndexRange)


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
        this.CompileAndVerify compilationManager entryPoints.[1] [Error ErrorCode.OtherEntryPointExists]
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore


    [<Fact>]
    member this.``Entry point validation`` () =

        for entryPoint in LinkingTests.ReadAndChunkSourceFile "ValidEntryPoints.qs" do
            this.CompileAndVerify compilationManager entryPoint []
        this.Expect "EntryPointInLibrary" [Warning WarningCode.EntryPointInLibrary]


    [<Fact>]
    member this.``Entry point argument name verification`` () =

        let tests = LinkingTests.ReadAndChunkSourceFile "EntryPointDiagnostics.qs" 
        this.CompileAndVerify compilationManager tests.[0] [Error ErrorCode.DuplicateEntryPointArgumentName]
        this.CompileAndVerify compilationManager tests.[1] [Error ErrorCode.DuplicateEntryPointArgumentName]
        this.CompileAndVerify compilationManager tests.[2] [Error ErrorCode.DuplicateEntryPointArgumentName]
        this.CompileAndVerify compilationManager tests.[3] [Warning WarningCode.ReservedEntryPointArgumentName]
        this.CompileAndVerify compilationManager tests.[4] [Warning WarningCode.ReservedEntryPointArgumentName]


    [<Fact>]
    member this.``Entry point specialization verification`` () =

        for entryPoint in LinkingTests.ReadAndChunkSourceFile "EntryPointSpecializations.qs" do
            this.CompileAndVerify compilationManager entryPoint [Error ErrorCode.InvalidEntryPointSpecialization]


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
    member this.``Entry point return type restriction for quantum processors`` () =

        let tests = LinkingTests.ReadAndChunkSourceFile "EntryPointDiagnostics.qs" 
        let compilationManager =
            new CompilationUnitManager
                (Action<_> (fun ex -> failwith ex.Message),
                 isExecutable = true,
                 capability = BasicQuantumFunctionality)
        let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile
        
        this.CompileAndVerify compilationManager tests.[5]  []
        this.CompileAndVerify compilationManager tests.[6]  []
        this.CompileAndVerify compilationManager tests.[7]  []
        this.CompileAndVerify compilationManager tests.[8]  []
        this.CompileAndVerify compilationManager tests.[9]  [Warning WarningCode.NonResultTypeReturnedInEntryPoint]
        this.CompileAndVerify compilationManager tests.[10] [Warning WarningCode.NonResultTypeReturnedInEntryPoint]
        this.CompileAndVerify compilationManager tests.[11] [Warning WarningCode.NonResultTypeReturnedInEntryPoint]
        this.CompileAndVerify compilationManager tests.[12] [Warning WarningCode.NonResultTypeReturnedInEntryPoint]


    [<Fact>]
    member this.``Entry point argument and return type verification`` () =

        this.Expect "InvalidEntryPoint1"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint2"  [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint3"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint4"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint5"  [Error ErrorCode.QubitTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint6"  [Error ErrorCode.QubitTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint7"  [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint8"  [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint9"  [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint10" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint11" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint12" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint13" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint14" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint15" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint16" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint17" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint18" [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint19" [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint20" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint21" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint22" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint23" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint24" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint25" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint26" [Error ErrorCode.CallableTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint27" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint28" [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint29" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint30" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint31" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint32" [Error ErrorCode.UserDefinedTypeInEntryPointSignature]

        this.Expect "InvalidEntryPoint33" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint34" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint35" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint36" [Error ErrorCode.CallableTypeInEntryPointSignature]
        this.Expect "InvalidEntryPoint37" [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint38" [Error ErrorCode.InnerTupleInEntryPointArgument]
        this.Expect "InvalidEntryPoint39" [Error ErrorCode.ArrayOfArrayInEntryPointArgument]
        this.Expect "InvalidEntryPoint40" [Error ErrorCode.ArrayOfArrayInEntryPointArgument]
        this.Expect "InvalidEntryPoint41" [Error ErrorCode.ArrayOfArrayInEntryPointArgument]


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
        let manager = new CompilationUnitManager()

        let sourceCompilation = this.BuildContent (manager, chunks.[7])
        let namespaces =
            sourceCompilation.BuiltCompilation.Namespaces
            |> Seq.filter (fun ns -> ns.Name.StartsWith Signatures.InternalRenamingNs)

        let references = createReferences ["InternalRenaming1.dll", namespaces
                                           "InternalRenaming2.dll", namespaces]
        let referenceCompilation = this.BuildContent (manager, "", references)
        let callables = GlobalCallableResolutions referenceCompilation.BuiltCompilation.Namespaces

        let decorator = new NameDecorator("QsRef")
        for idx = 0 to references.Declarations.Count - 1 do
            let name = decorator.Decorate (qualifiedName Signatures.InternalRenamingNs "Foo", idx)
            let specializations = callables.[name].Specializations
            Assert.Equal (4, specializations.Length)
            Assert.True (specializations |> Seq.forall (fun s -> s.Source = callables.[name].Source))


    [<Fact>]
    member this.``Combine conflicting syntax trees`` () = 

        let checkInvalidCombination (conflicts : ImmutableDictionary<_, _>) sources =
            let mutable combined = ImmutableArray<QsNamespace>.Empty
            let trees = sources |> Seq.map this.BuildReference |> Seq.toArray
            let onError _ (args : _[]) = 
                Assert.Equal(2, args.Length)
                Assert.True(conflicts.ContainsKey(args.[0]))
                Assert.Equal(conflicts.[args.[0]], args.[1])
            let success = References.CombineSyntaxTrees(&combined, 0, new Action<_,_>(onError), trees)
            Assert.False(success, "combined conflicting syntax trees")

        let source =  sprintf "Reference%i.dll"
        let chunks = LinkingTests.ReadAndChunkSourceFile "ReferenceLinking.qs"
        let buildDict (args : _ seq) = args.ToImmutableDictionary(fst, snd)

        let expectedErrs = buildDict [
            ("Microsoft.Quantum.Testing.Linking.BigEndian", "Reference1.dll, Reference2.dll")
            ("Microsoft.Quantum.Testing.Linking.Foo",       "Reference1.dll, Reference2.dll")
            ("Microsoft.Quantum.Testing.Linking.Bar",       "Reference1.dll, Reference2.dll")
        ] 
        checkInvalidCombination expectedErrs [
            (source 1, chunks.[0]); 
            (source 2, chunks.[0]); 
        ]

        let expectedErrs = buildDict [
            ("Microsoft.Quantum.Testing.Linking.BigEndian", "Reference1.dll, Reference2.dll")
        ] 
        checkInvalidCombination expectedErrs [
            (source 1, chunks.[0]); 
            (source 2, chunks.[2]); 
        ]
        checkInvalidCombination expectedErrs [
            (source 1, chunks.[0]); 
            (source 2, chunks.[3]); 
        ]
        checkInvalidCombination expectedErrs [
            (source 1, chunks.[2]); 
            (source 2, chunks.[3]); 
            (source 3, chunks.[4]); 
        ]
        checkInvalidCombination expectedErrs [
            (source 1, chunks.[3]); 
            (source 2, chunks.[5]); 
        ]


    [<Fact>]
    member this.``Combine syntax trees to a valid reference`` () = 

        let checkValidCombination (sources : ImmutableDictionary<_, _>) =
            let mutable combined = ImmutableArray<QsNamespace>.Empty
            let trees = sources |> Seq.map (fun kv -> this.BuildReference (kv.Key, fst kv.Value)) |> Seq.toArray
            let sourceIndex = (trees |> Seq.mapi (fun i (struct (x, _)) -> (x, i))).ToImmutableDictionary(fst, snd)
            let onError _ _ = Assert.False(true, "diagnostics generated upon combining syntax trees")
            let success = References.CombineSyntaxTrees(&combined, 0, new Action<_,_>(onError), trees)
            Assert.True(success, "failed to combine syntax trees")

            let decorator = new NameDecorator("QsRef")
            let undecorate (assertUndecorated : bool) (fullName : QsQualifiedName, srcIdx) =
                let name = decorator.Undecorate fullName.Name
                if name <> null then 
                    Assert.Equal<string>(decorator.Decorate(name, srcIdx), fullName.Name)
                    {Namespace = fullName.Namespace; Name = name}
                else Assert.False(assertUndecorated, sprintf "name %s is not decorated" (fullName.ToString())); fullName

            /// Verifies that internal names have been decorated appropriately, 
            /// and that the correct source is set. 
            let AssertSource (fullName : QsQualifiedName, source, modifier : _ option) = 
                match sources.TryGetValue source with 
                | true, (_, decls : _ Set) ->
                    let idx = sourceIndex.[source]
                    let name = 
                        if modifier.IsNone then undecorate false (fullName, idx)
                        elif modifier.Value = Internal then undecorate true (fullName, idx)
                        else fullName
                    Assert.True(decls.Contains name)
                | false, _ -> Assert.True(false, "wrong source")

            let onTypeDecl (tDecl : QsCustomType) = 
                AssertSource (tDecl.FullName, Source.assemblyOrCode tDecl.Source, Some tDecl.Modifiers.Access)
                tDecl
            let onCallableDecl (cDecl : QsCallable) = 
                AssertSource (cDecl.FullName, Source.assemblyOrCode cDecl.Source, Some cDecl.Modifiers.Access)
                cDecl
            let onSpecDecl (sDecl : QsSpecialization) = 
                AssertSource (sDecl.Parent, Source.assemblyOrCode sDecl.Source, None)
                sDecl
            let checker = new CheckDeclarations(onTypeDecl, onCallableDecl, onSpecDecl)
            checker.OnCompilation(QsCompilation.New (combined, ImmutableArray<QsQualifiedName>.Empty)) |> ignore

        let source =  sprintf "Reference%i.dll"
        let chunks = LinkingTests.ReadAndChunkSourceFile "ReferenceLinking.qs"
        let fullName (ns, name) = {Namespace = ns; Name = name}
        let buildDict (args : _ seq) = args.ToImmutableDictionary(fst, snd)

        let declInSource1 = new Set<_>([
            ("Microsoft.Quantum.Testing.Linking", "BigEndian") |> fullName
            ("Microsoft.Quantum.Testing.Linking", "Foo")       |> fullName
            ("Microsoft.Quantum.Testing.Linking", "Bar")       |> fullName
        ])
        checkValidCombination (buildDict [
            (source 1, (chunks.[0], declInSource1))
            (source 2, (chunks.[1], declInSource1))
        ])
        checkValidCombination (buildDict [
            (source 1, (chunks.[1], declInSource1))
            (source 2, (chunks.[1], declInSource1))
        ])
        checkValidCombination (buildDict [
            (source 1, (chunks.[2], declInSource1))
            (source 2, (chunks.[4], declInSource1))
        ])
        checkValidCombination (buildDict [
            (source 1, (chunks.[3], declInSource1))
            (source 2, (chunks.[4], declInSource1))
        ])

        let declInSource2 = new Set<_>([
            ("Microsoft.Quantum.Testing.Linking.Core", "BigEndian") |> fullName
            ("Microsoft.Quantum.Testing.Linking.Core", "Foo")       |> fullName
            ("Microsoft.Quantum.Testing.Linking.Core", "Bar")       |> fullName
        ])
        checkValidCombination (buildDict [
            (source 1, (chunks.[0], declInSource1))
            (source 2, (chunks.[6], declInSource2))
        ])
