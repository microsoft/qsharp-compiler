// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open System
open System.Collections.Generic
open System.Collections.Immutable
open System.IO
open System.Linq
open Xunit

type CompilerTests(compilation: CompilationUnitManager.Compilation) =
    let syntaxTree =
        let mutable compilation = compilation.BuiltCompilation
        CodeGeneration.GenerateFunctorSpecializations(compilation, &compilation) |> ignore // the functor generation is expected to fail for certain cases
        compilation.Namespaces

    let callables = syntaxTree |> GlobalCallableResolutions
    let types = syntaxTree |> GlobalTypeResolutions

    let diagnostics =
        let callableStart (c: QsCallable) =
            let attributes =
                match c.Kind with
                | TypeConstructor -> types[c.FullName].Attributes
                | _ -> c.Attributes

            if attributes.IsEmpty then
                c.Location.ValueOrApply(fun _ -> failwith "missing position information").Offset
            else
                attributes |> Seq.map (fun a -> a.Offset) |> Seq.sort |> Seq.head

        [
            for file in compilation.SourceFiles do
                let fileCallables = callables |> Seq.filter (fun c -> Source.assemblyOrCodeFile c.Value.Source = file)

                let locations =
                    fileCallables
                    |> Seq.map (fun c -> c.Key, callableStart c.Value)
                    |> Seq.sortBy snd
                    |> ImmutableArray.CreateRange

                let mutable diagnostics = compilation.Diagnostics file |> Seq.sortBy (fun d -> d.Range.Start.ToQSharp())

                for i = 1 to locations.Length do
                    let inCurrent (d: Diagnostic) =
                        i = locations.Length || d.Range.Start.ToQSharp() < snd locations[i]

                    let name, start = locations[i - 1]
                    let ds = Seq.takeWhile inCurrent diagnostics |> ImmutableArray.CreateRange
                    diagnostics <- Seq.skipWhile inCurrent diagnostics

                    for d in ds do
                        // Convert the range to be relative to the callable start line so it can be used in assertions.
                        d.Range.Start.Line <- d.Range.Start.Line - start.Line
                        d.Range.End.Line <- d.Range.End.Line - start.Line

                    yield name, ds
        ]
        |> readOnlyDict

    let verifyDiagnosticsOfSeverity severity name (expected: IEnumerable<_>) =
        let exists, diag = diagnostics.TryGetValue name
        Assert.True(exists, sprintf "no entry found for %s.%s" name.Namespace name.Name)

        let got =
            diag.Where(fun d -> d.Severity = severity)
            |> Seq.choose (fun d ->
                match Diagnostics.TryGetCode d.Code.Value.Second with
                | true, code -> Some code
                | false, _ -> None)

        let codeMismatch = expected.ToImmutableHashSet().SymmetricExcept got
        let gotLookup = got.ToLookup(new Func<_, _>(id))
        let expectedLookup = expected.ToLookup(new Func<_, _>(id))
        let nrMismatch = gotLookup.Where(fun g -> g.Count() <> expectedLookup.[g.Key].Count())

        Assert.False(
            codeMismatch.Any() || nrMismatch.Any(),
            sprintf
                "%A code mismatch for %s.%s \nexpected: %s\ngot: %s\nmessages: \n\t%s"
                severity
                name.Namespace
                name.Name
                (String.Join(", ", expected))
                (String.Join(", ", got))
                (String.Join("\n\t", diag.Where(fun d -> d.Severity = severity).Select(fun d -> d.Message)))
        )


    member this.Verify(name, expected: IEnumerable<ErrorCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity (Nullable DiagnosticSeverity.Error) name expected

    member this.Verify(name, expected: IEnumerable<WarningCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity (Nullable DiagnosticSeverity.Warning) name expected

    member this.Verify(name, expected: IEnumerable<InformationCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity (Nullable DiagnosticSeverity.Information) name expected

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

        if other.Any() then NotImplementedException "unknown diagnostics item to verify" |> raise

    static member Compile(srcFolder, fileNames, ?references, ?capability, ?isExecutable) =
        let references = defaultArg references []
        let isExecutable = defaultArg isExecutable false
        let capabilityName = Option.bind TargetCapability.name capability

        let files =
            fileNames
            |> Seq.map (fun name ->
                let path = Path.Combine(srcFolder, name) |> Path.GetFullPath
                Uri path, File.ReadAllText path)
            |> dict

        let props =
            dict [ MSBuildProperties.ResolvedTargetCapability, Option.toObj capabilityName
                   if isExecutable then MSBuildProperties.ResolvedQsharpOutputType, AssemblyConstants.QsharpExe ]
            |> ProjectProperties

        let exceptions = ResizeArray()
        use manager = new CompilationUnitManager(props, Action<_> exceptions.Add)
        manager.AddOrUpdateSourceFilesAsync(CompilationUnitManager.InitializeFileManagers files) |> ignore

        manager.UpdateReferencesAsync(ProjectManager.LoadReferencedAssemblies references |> References)
        |> ignore

        let compilation = manager.Build()
        if exceptions.Count > 0 then AggregateException exceptions |> raise else compilation
