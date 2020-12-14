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
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open Xunit


type CompilerTests(compilation: CompilationUnitManager.Compilation) =

    let syntaxTree =
        let mutable compilation = compilation.BuiltCompilation
        CodeGeneration.GenerateFunctorSpecializations(compilation, &compilation) |> ignore // the functor generation is expected to fail for certain cases
        compilation.Namespaces

    let callables = syntaxTree |> GlobalCallableResolutions
    let types = syntaxTree |> GlobalTypeResolutions

    let diagnostics =
        let getCallableStart (c: QsCallable) =
            let attributes =
                match c.Kind with
                | TypeConstructor -> types.[c.FullName].Attributes
                | _ -> c.Attributes

            if attributes.Length = 0
            then (c.Location.ValueOrApply(fun _ -> failwith "missing position information")).Offset
            else attributes |> Seq.map (fun att -> att.Offset) |> Seq.sort |> Seq.head

        [
            for file in compilation.SourceFiles do
                let containedCallables =
                    callables.Where(fun kv -> kv.Value.SourceFile = file && kv.Value.Location <> Null)

                let locations =
                    containedCallables.Select(fun kv -> kv.Key, kv.Value |> getCallableStart)
                    |> Seq.sortBy snd
                    |> Seq.toArray

                let mutable containedDiagnostics =
                    compilation.Diagnostics file |> Seq.sortBy (fun d -> d.Range.Start.ToQSharp())

                for i = 1 to locations.Length do
                    let key = fst locations.[i - 1]

                    if i < locations.Length then
                        let withinCurrentDeclaration (d: Diagnostic) =
                            d.Range.Start.ToQSharp() < snd locations.[i]

                        yield key, containedDiagnostics.TakeWhile(withinCurrentDeclaration).ToImmutableArray()
                        containedDiagnostics <- containedDiagnostics.SkipWhile(withinCurrentDeclaration)
                    else
                        yield key, containedDiagnostics.ToImmutableArray()
        ]
            .ToImmutableDictionary(fst, snd)

    let VerifyDiagnosticsOfSeverity severity name (expected: IEnumerable<_>) =
        let exists, diag = diagnostics.TryGetValue name
        Assert.True(exists, sprintf "no entry found for %s.%s" name.Namespace name.Name)

        let got =
            diag.Where(fun d -> d.Severity = severity)
            |> Seq.choose (fun d ->
                match Diagnostics.TryGetCode d.Code with
                | true, code -> Some code
                | false, _ -> None)

        let codeMismatch = expected.ToImmutableHashSet().SymmetricExcept got
        let gotLookup = got.ToLookup(new Func<_, _>(id))
        let expectedLookup = expected.ToLookup(new Func<_, _>(id))
        let nrMismatch = gotLookup.Where(fun g -> g.Count() <> expectedLookup.[g.Key].Count())

        Assert.False
            (codeMismatch.Any() || nrMismatch.Any(),
             sprintf "%A code mismatch for %s.%s \nexpected: %s\ngot: %s" severity name.Namespace name.Name
                 (String.Join(", ", expected)) (String.Join(", ", got)))


    member this.Verify(name, expected: IEnumerable<ErrorCode>) =
        let expected = expected.Select(fun code -> int code)
        VerifyDiagnosticsOfSeverity DiagnosticSeverity.Error name expected

    member this.Verify(name, expected: IEnumerable<WarningCode>) =
        let expected = expected.Select(fun code -> int code)
        VerifyDiagnosticsOfSeverity DiagnosticSeverity.Warning name expected

    member this.Verify(name, expected: IEnumerable<InformationCode>) =
        let expected = expected.Select(fun code -> int code)
        VerifyDiagnosticsOfSeverity DiagnosticSeverity.Information name expected

    member this.VerifyDiagnostics(name, expected: IEnumerable<DiagnosticItem>) =
        let errs =
            expected
            |> Seq.choose (function
                | Error err -> Some err
                | _ -> None)

        let wrns =
            expected
            |> Seq.choose (function
                | Warning wrn -> Some wrn
                | _ -> None)

        let infs =
            expected
            |> Seq.choose (function
                | Information inf -> Some inf
                | _ -> None)

        this.Verify(name, errs)
        this.Verify(name, wrns)
        this.Verify(name, infs)

        let other =
            expected
            |> Seq.choose (function
                | Warning _
                | Error _ -> None
                | item -> Some item)

        if other.Any()
        then NotImplementedException "unknown diagnostics item to verify" |> raise


    static member Compile(srcFolder, fileNames, ?references, ?capability) =
        let references = defaultArg references []
        let capability = defaultArg capability FullComputation
        let paths = fileNames |> Seq.map (fun file -> Path.Combine(srcFolder, file) |> Path.GetFullPath)
        let mutable exceptions = []
        use manager = new CompilationUnitManager((fun e -> exceptions <- e :: exceptions), capability = capability)

        paths.ToImmutableDictionary(Uri, File.ReadAllText)
        |> CompilationUnitManager.InitializeFileManagers
        |> manager.AddOrUpdateSourceFilesAsync
        |> ignore

        references
        |> ProjectManager.LoadReferencedAssemblies
        |> References
        |> manager.UpdateReferencesAsync
        |> ignore

        let compilation = manager.Build()

        if not <| List.isEmpty exceptions
        then exceptions |> List.rev |> AggregateException |> raise

        compilation
