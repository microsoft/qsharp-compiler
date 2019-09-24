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
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit
open Xunit.Abstractions


type CompilerTests (srcFolder, files, output:ITestOutputHelper) = 

    let compilation = 
        let compileFiles (files : IEnumerable<_>) =
            let mgr = new CompilationUnitManager(fun ex -> failwith ex.Message)
            files.ToImmutableDictionary(Path.GetFullPath >> Uri, File.ReadAllText) 
            |> CompilationUnitManager.InitializeFileManagers
            |> mgr.AddOrUpdateSourceFilesAsync 
            |> ignore
            mgr.Build() 
        files |> Seq.map (fun file -> Path.Combine (srcFolder, file)) |> compileFiles 

    let syntaxTree = 
        match compilation.SyntaxTree.Values |> FunctorGeneration.GenerateFunctorSpecializations with 
        | _, tree -> tree // the functor generation is expected to fail for certain cases

    let callables = syntaxTree |> GlobalCallableResolutions
    let types = syntaxTree |> GlobalTypeResolutions

    let diagnostics =
        let getCallableStart (c : QsCallable) = 
            if c.Attributes.Length = 0 then c.Location.Offset 
            else c.Attributes |> Seq.map (fun att -> att.Offset) |> Seq.sort |> Seq.head
        [for file in compilation.SourceFiles do
            let containedCallables = callables.Where(fun kv -> kv.Value.SourceFile.Value = file.Value)
            let locations = containedCallables.Select(fun kv -> kv.Key, kv.Value |> getCallableStart) |> Seq.sortBy snd |> Seq.toArray
            let mutable containedDiagnostics = compilation.Diagnostics file |> Seq.sortBy (fun d -> DiagnosticTools.AsTuple d.Range.Start)
            
            for i = 1 to locations.Length do
                let key = fst locations.[i-1]
                if i < locations.Length then 
                    let withinCurrentDeclaration (d : Diagnostic) = 
                        Utils.IsSmallerThan(d.Range.Start, snd locations.[i] |> DiagnosticTools.AsPosition)
                    yield key, containedDiagnostics.TakeWhile(withinCurrentDeclaration).ToImmutableArray()
                    containedDiagnostics <- containedDiagnostics.SkipWhile(withinCurrentDeclaration)
                else yield key, containedDiagnostics.ToImmutableArray()
        ].ToImmutableDictionary(fst, snd)
             
    let VerifyDiagnostics severity name (expected : IEnumerable<_>) = 
        let exists, diag = diagnostics.TryGetValue name
        Assert.True(exists, sprintf "no entry found for %s.%s" name.Namespace.Value name.Name.Value)
        let got = 
            diag.Where(fun d -> d.Severity = severity) 
            |> Seq.choose (fun d -> Diagnostics.TryGetCode d.Code |> function 
                | true, code -> Some code 
                | false, _ -> None)
        let codeMismatch = expected.ToImmutableHashSet().SymmetricExcept got
        let gotLookup = got.ToLookup(new Func<_,_>(id))
        let expectedLookup = expected.ToLookup(new Func<_,_>(id))
        let nrMismatch = gotLookup.Where (fun g -> g.Count() <> expectedLookup.[g.Key].Count())
        Assert.False(codeMismatch.Any() || nrMismatch.Any(), 
            sprintf "%A code mismatch for %s.%s \nexpected: %s\ngot: %s" 
                severity name.Namespace.Value name.Name.Value (String.Join(", ", expected)) (String.Join(", ", got)))


    member this.Verify (name, expected : IEnumerable<ErrorCode>) = 
        let expected = expected.Select(fun code -> int code) 
        VerifyDiagnostics DiagnosticSeverity.Error name expected

    member this.Verify (name, expected : IEnumerable<WarningCode>) = 
        let expected = expected.Select(fun code -> int code) 
        VerifyDiagnostics DiagnosticSeverity.Warning name expected

    member this.Verify (name, expected : IEnumerable<InformationCode>) = 
        let expected = expected.Select(fun code -> int code) 
        VerifyDiagnostics DiagnosticSeverity.Information name expected

    member this.Verify (name, expected : IEnumerable<DiagnosticItem>) = 
        let errs = expected |> Seq.choose (function | Error err -> Some err | _ -> None)
        let wrns = expected |> Seq.choose (function | Warning wrn -> Some wrn | _ -> None)
        let infs = expected |> Seq.choose (function | Information inf -> Some inf | _ -> None)
        this.Verify (name, errs)
        this.Verify (name, wrns)
        this.Verify (name, infs)
        let other = expected |> Seq.choose (function | Warning _ | Error _ -> None | item -> Some item)
        if other.Any() then NotImplementedException "unknown diagnostics item to verify" |> raise