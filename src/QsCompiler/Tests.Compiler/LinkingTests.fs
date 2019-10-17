// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions


type LinkingTests (output:ITestOutputHelper) =
    inherit CompilerTests(CompilerTests.Compile (Path.Combine ("TestCases", "LinkingTests" )) ["Core.qs"; "InvalidEntryPoints.qs"], output)

    let getTempFile () = new Uri(Path.GetFullPath(Path.GetRandomFileName()))
    let diagnostics = new ConcurrentDictionary<Uri, Diagnostic[]>()
    let pushDiagnostics (param : PublishDiagnosticParams) = 
        let replace _ _ = param.Diagnostics
        diagnostics.AddOrUpdate (param.Uri, param.Diagnostics, new Func<_, _, _>(replace))
        |> ignore

    let compilation = 
        new CompilationUnitManager(
            new Action<Exception> (fun ex -> failwith ex.Message),
            new Action<PublishDiagnosticParams> (pushDiagnostics))
    
    let getManager uri content = 
        CompilationUnitManager.InitializeFileManager(uri, content, compilation.PublishDiagnostics, compilation.LogException)

    do  let core = Path.Combine ("TestCases", "LinkingTests", "Core.qs") |> Path.GetFullPath 
        let file = getManager (new Uri(core)) (File.ReadAllText core)
        compilation.AddOrUpdateSourceFileAsync(file) |> ignore

    member private this.CompileAndVerify input = 
        async {
            let fileId = getTempFile()
            let file = getManager fileId input
            do! compilation.AddOrUpdateSourceFileAsync(file) |> Async.AwaitTask
            match diagnostics.TryRemove fileId with 
            | true, msgs -> Assert.Empty(msgs)
            | false, _ -> Assert.NotNull(null, "failed to get diagnostics")
        }

    member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
        let ns = "Microsoft.Quantum.Testing.EntryPoints" |> NonNullable<_>.New
        let name = name |> NonNullable<_>.New
        this.Verify (QsQualifiedName.New (ns, name), diag)


    [<Fact>]
    member this.``Entry point verification`` () = 

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
        //this.CompileAndVerify "" |> Async.RunSynchronously