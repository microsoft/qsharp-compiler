// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.VisualStudio.LanguageServer.Protocol
open System
open System.Collections.Generic
open System.Collections.Immutable
open Xunit

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Utils

type CompilerTests(compilation: CompilationUnitManager.Compilation) =
    let syntaxTree =
        let mutable compilation = compilation.BuiltCompilation
        CodeGeneration.GenerateFunctorSpecializations(compilation, &compilation) |> ignore // the functor generation is expected to fail for certain cases
        compilation.Namespaces

    let callables = GlobalCallableResolutions syntaxTree
    let types = GlobalTypeResolutions syntaxTree

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

    member this.AssertDiagnostics(name, expected: (DiagnosticItem * Range option) seq) =
        let expectedDict = Dictionary()

        for e in expected do
            match expectedDict.TryGetValue e with
            | true, count -> expectedDict[e] <- count + 1
            | false, _ -> expectedDict[e] <- 1

        let actual =
            diagnostics.TryGetValue name
            |> tryOption
            |> Option.get
            |> Seq.map (fun d ->
                let code = Diagnostics.TryGetCode d.Code.Value.Second |> snd

                let item =
                    match d.Severity.Value with
                    | DiagnosticSeverity.Error -> enum code |> Error
                    | DiagnosticSeverity.Warning -> enum code |> Warning
                    | DiagnosticSeverity.Information -> enum code |> Information
                    | _ -> failwith "Invalid severity."

                item, d.Range.ToQSharp())

        for item, range in actual do
            match expectedDict.TryGetValue((item, Some range)) with
            | true, 1 -> assert expectedDict.Remove((item, Some range))
            | true, count -> expectedDict[(item, Some range)] <- count - 1
            | false, _ ->
                match expectedDict.TryGetValue((item, None)) with
                | true, 1 -> assert expectedDict.Remove((item, None))
                | true, count -> expectedDict[(item, None)] <- count - 1
                | false, _ -> failwith "Unexpected diagnostic."

        Assert.Empty expectedDict

    // TODO: Remove this.
    member this.VerifyDiagnostics(name, expected) =
        this.AssertDiagnostics(name, Seq.map (fun e -> e, None) expected)

    // TODO: Remove this.
    static member Compile(srcFolder, fileNames, ?references, ?capability, ?isExecutable) =
        let references = defaultArg references []
        let output = if Option.contains true isExecutable then TestUtils.Exe else TestUtils.Library
        TestUtils.buildFiles srcFolder fileNames references capability output
