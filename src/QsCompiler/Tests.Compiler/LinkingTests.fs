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
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Monomorphization
open Microsoft.Quantum.QsCompiler.Transformations.MonomorphizationValidation
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit
open Xunit.Abstractions


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests" )) ["Core.qs"; "InvalidEntryPoints.qs"], output)

    static let typeMap =
        [|
            "Unit", QsTypeKind.UnitType;
            "Int", QsTypeKind.Int;
            "Double", QsTypeKind.Double;
            "String", QsTypeKind.String;
            "Qubit", QsTypeKind.Qubit;
            "Qubit[]", ResolvedType.New QsTypeKind.Qubit |> QsTypeKind.ArrayType;
        |] 
        |> Seq.map (fun (k, v) -> k, ResolvedType.New v) |> dict

    let compilationManager = new CompilationUnitManager(new Action<Exception> (fun ex -> failwith ex.Message))

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let getManager uri content = CompilationUnitManager.InitializeFileManager(uri, content, compilationManager.PublishDiagnostics, compilationManager.LogException)

    do  let addOrUpdateSourceFile filePath = getManager (new Uri(filePath)) (File.ReadAllText filePath) |> compilationManager.AddOrUpdateSourceFileAsync |> ignore
        Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath |> addOrUpdateSourceFile
        Path.Combine ("TestCases", "LinkingTests", "Generics.qs") |> Path.GetFullPath |> addOrUpdateSourceFile

    static member private SignatureCheck targetSignatures compilation =

        let callableSigs =
            compilation.Namespaces
            |> SyntaxExtensions.Callables
            |> Seq.map (fun call -> (call.FullName, call.Signature.ArgumentType, call.Signature.ReturnType))

        let doesCallMatchSig call signature =
            let (call_fullName : QsQualifiedName), call_argType, call_rtrnType = call
            let (sig_fullName : QsQualifiedName), sig_argType, sig_rtrnType = signature

            call_fullName.Namespace.Value = sig_fullName.Namespace.Value &&
            call_fullName.Name.Value.EndsWith sig_fullName.Name.Value &&
            call_argType = sig_argType &&
            call_rtrnType = sig_rtrnType

        let makeArgsString (args : ResolvedType) =
            match args.Resolution with
            | QsTypeKind.UnitType -> "()"
            | _ -> args |> (ExpressionToQs () |> ExpressionTypeToQs).Apply

        (*Tests that all target signatures are present*)
        for targetSig in targetSignatures do
            let sig_fullName, sig_argType, sig_rtrnType = targetSig
            callableSigs
            |> Seq.exists (fun callSig -> doesCallMatchSig callSig targetSig)
            |> (fun x -> Assert.True (x, sprintf "Expected but did not find: %s.%s %s : %A" sig_fullName.Namespace.Value sig_fullName.Name.Value (makeArgsString sig_argType) sig_rtrnType.Resolution))

        (*Tests that *only* targeted signatures are present*)
        for callSig in callableSigs do
            let sig_fullName, sig_argType, sig_rtrnType = callSig
            targetSignatures
            |> Seq.exists (fun targetSig -> doesCallMatchSig callSig targetSig)
            |> (fun x -> Assert.True (x, sprintf "Found unexpected callable: %s.%s %s : %A" sig_fullName.Namespace.Value sig_fullName.Name.Value (makeArgsString sig_argType) sig_rtrnType.Resolution))

    static member private GetEntryPoints fileName =
        let validEntryPoints = Path.Combine ("TestCases", "LinkingTests", fileName) |> File.ReadAllText
        validEntryPoints.Split ([|"==="|], StringSplitOptions.RemoveEmptyEntries)

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

    member private this.CompileAndTestMonomorphization input =

        let fileId = getTempFile()
        let file = getManager fileId input

        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        let compilationDataStructures = compilationManager.Build()
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore

        compilationDataStructures.Diagnostics() |> Seq.exists (fun d -> d.IsError()) |> Assert.False

        Assert.NotNull compilationDataStructures.BuiltCompilation
        let monomorphicCompilation = MonomorphizationTransformation.Apply compilationDataStructures.BuiltCompilation

        Assert.NotNull monomorphicCompilation
        MonomorphizationValidationTransformation.Apply monomorphicCompilation
        monomorphicCompilation


    [<Fact>]
    member this.``Monomorphization`` () =

        let makeSig input =
            let ns, name, args, rtrn = input
            let fullName = { Namespace = NonNullable<string>.New ns; Name = NonNullable<string>.New name }
            let argType =
                if Array.isEmpty args then
                    typeMap.["Unit"]
                else
                    args |> Seq.map (fun typ -> typeMap.[typ]) |> ImmutableArray.ToImmutableArray |> QsTypeKind.TupleType |> ResolvedType.New
            let returnType = typeMap.[rtrn]
            (fullName, argType, returnType)

        let genericsNs = "Microsoft.Quantum.Testing.Generics"
        let monomorphizationNs = "Microsoft.Quantum.Testing.Monomorphization"
        let targetSignatures =
            [|
                [| (*Test Case 1*)
                    monomorphizationNs, "Test1", [||], "Unit";
                    genericsNs, "Test1Main", [||], "Unit";

                    genericsNs, "BasicGeneric", [|"Double"; "Int"|], "Unit";
                    genericsNs, "BasicGeneric", [|"String"; "String"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Unit"; "Unit"|], "Unit";
                    genericsNs, "BasicGeneric", [|"String"; "Double"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Int"; "Double"|], "Unit";
                    genericsNs, "NoArgsGeneric", [||], "Double";
                    genericsNs, "ReturnGeneric", [|"Double"; "String"; "Int"|], "Int";
                    genericsNs, "ReturnGeneric", [|"String"; "Int"; "String"|], "String";
                |];
                [| (*Test Case 2*)
                    monomorphizationNs, "Test2", [||], "Unit";
                    genericsNs, "Test2Main", [||], "Unit";

                    genericsNs, "ArrayGeneric", [|"Qubit"; "String"|], "Int";
                    genericsNs, "ArrayGeneric", [|"Qubit"; "Int"|], "Int";
                    genericsNs, "GenericCallsGeneric", [|"Qubit"; "Int"|], "Unit";
                |];
                [| (*Test Case 3*)
                    monomorphizationNs, "Test3", [||], "Unit";
                    genericsNs, "Test3Main", [||], "Unit";

                    genericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Qubit[]"|], "Unit";
                    genericsNs, "GenericCallsSpecializations", [|"Double"; "String"; "Double"|], "Unit";
                    genericsNs, "GenericCallsSpecializations", [|"String"; "Int"; "Unit"|], "Unit";

                    genericsNs, "BasicGeneric", [|"Qubit[]"; "Qubit[]"|], "Unit";
                    genericsNs, "BasicGeneric", [|"String"; "Qubit[]"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Double"; "String"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Qubit[]"; "Double"|], "Unit";
                    genericsNs, "BasicGeneric", [|"String"; "Double"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Qubit[]"; "Unit"|], "Unit";
                    genericsNs, "BasicGeneric", [|"Int"; "Unit"|], "Unit";
                    genericsNs, "BasicGeneric", [|"String"; "Int"|], "Unit";
                    
                    genericsNs, "ArrayGeneric", [|"Qubit"; "Qubit[]"|], "Int";
                    genericsNs, "ArrayGeneric", [|"Qubit"; "Double"|], "Int";
                    genericsNs, "ArrayGeneric", [|"Qubit"; "Unit"|], "Int";
                |]
            |] |> Seq.map (fun case -> Seq.map (fun _sig -> makeSig _sig) case)

        for testCase in LinkingTests.GetEntryPoints "Monomorphization.qs" |> Seq.zip targetSignatures do
            this.CompileAndTestMonomorphization (snd testCase) |>
            LinkingTests.SignatureCheck (fst testCase)


    [<Fact>]
    member this.``Fail on multiple entry points`` () =

        let entryPoints = LinkingTests.GetEntryPoints "ValidEntryPoints.qs"
        Assert.True (entryPoints.Length > 1)

        let fileId = getTempFile()
        let file = getManager fileId entryPoints.[0]
        compilationManager.AddOrUpdateSourceFileAsync(file) |> ignore
        this.CompileAndVerify entryPoints.[1] [Error ErrorCode.MultipleEntryPoints]
        compilationManager.TryRemoveSourceFileAsync(fileId, false) |> ignore


    [<Fact>]
    member this.``Entry point specialization verification`` () =

        for entryPoint in LinkingTests.GetEntryPoints "EntryPointSpecializations.qs" do
            this.CompileAndVerify entryPoint [Error ErrorCode.InvalidEntryPointSpecialization]

        for entryPoint in LinkingTests.GetEntryPoints "ValidEntryPoints.qs" do
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

