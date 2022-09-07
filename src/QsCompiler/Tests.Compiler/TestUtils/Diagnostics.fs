// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.Testing.Diagnostics

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open System.Collections.Generic
open System.Collections.Immutable

open Microsoft.Quantum.QsCompiler.DataTypes

open type CompilationUnitManager

type private MultiHashSet<'T when 'T: equality>(items) =
    // Invariant: For all (item, n) in dict, n > 0.
    let dict = Dictionary<'T, _>()

    do
        for item in items do
            dict[item] <- dict.GetValueOrDefault item + 1

    member _.IsEmpty = dict.Count = 0

    member _.Remove item =
        match dict.GetValueOrDefault item with
        | 0 -> false
        | 1 -> dict.Remove item
        | n ->
            dict[item] <- n - 1
            true

let private byDeclarationInFile declarations (diagnostics: Diagnostic seq) =
    let declarations = Seq.sortBy snd declarations |> ImmutableArray.CreateRange
    let mutable diagnostics = diagnostics |> Seq.sortBy (fun d -> d.Range.Start.ToQSharp())

    seq {
        for i = 1 to declarations.Length do
            let inCurrent (d: Diagnostic) =
                i = declarations.Length || d.Range.Start.ToQSharp() < snd declarations[i]

            let name, start = declarations[i - 1]
            let ds = Seq.takeWhile inCurrent diagnostics |> ImmutableArray.CreateRange
            diagnostics <- Seq.skipWhile inCurrent diagnostics

            for d in ds do
                // Convert the range to be relative to the callable start line so it can be used in assertions.
                d.Range.Start.Line <- d.Range.Start.Line - start.Line
                d.Range.End.Line <- d.Range.End.Line - start.Line

            yield name, ds
    }

let byDeclaration (compilation: Compilation) =
    // Functor generation is expected to fail in certain cases.
    let _, compilation' = CodeGeneration.GenerateFunctorSpecializations compilation.BuiltCompilation
    let callables = GlobalCallableResolutions compilation'.Namespaces
    let types = GlobalTypeResolutions compilation'.Namespaces

    let callablePosition (c: QsCallable) =
        let attributes = if c.Kind = TypeConstructor then types[c.FullName].Attributes else c.Attributes

        if attributes.IsEmpty then
            c.Location.ValueOrApply(fun _ -> failwith "Missing location.").Offset
        else
            attributes |> Seq.map (fun a -> a.Offset) |> Seq.min

    let fileDiagnostics file =
        let declarations =
            callables
            |> Seq.filter (fun c -> Source.assemblyOrCodeFile c.Value.Source = file)
            |> Seq.map (fun c -> c.Key, callablePosition c.Value)

        compilation.Diagnostics file |> byDeclarationInFile declarations

    Seq.collect fileDiagnostics compilation.SourceFiles |> dict

let private toDiagnosticItem (diagnostic: Diagnostic) =
    let _, code = Diagnostics.TryGetCode diagnostic.Code.Value.Second

    match diagnostic.Severity.Value with
    | DiagnosticSeverity.Error -> enum code |> Error
    | DiagnosticSeverity.Warning -> enum code |> Warning
    | DiagnosticSeverity.Information -> enum code |> Information
    | _ -> failwith "Severity not supported."

let private showRange (range: Range option) =
    match range with
    | None -> "with any range"
    | Some r -> $"from [Ln {r.Start.Line}, Col {r.Start.Column}] to [Ln {r.End.Line}, Col {r.End.Column}]"

let private showDiagnostics diagnostics =
    diagnostics
    |> Seq.map (fun (diagnostic, range, message) ->
        let message' = if String.IsNullOrWhiteSpace message then "" else $"\n  ({message})"
        $"- {diagnostic} {showRange range}{message'}")
    |> String.concat "\n"

let assertMatches expected (actual: Diagnostic seq) =
    let actual =
        actual
        |> Seq.map (fun d -> toDiagnosticItem d, d.Range.ToQSharp(), d.Message)
        |> ImmutableArray.CreateRange

    let fail () =
        let expected' = Seq.map (fun (d, r) -> d, r, "") expected |> showDiagnostics
        let actual' = Seq.map (fun (d, r, m) -> d, Some r, m) actual |> showDiagnostics
        failwith $"Diagnostics did not match.\n\nExpected:\n{expected'}\nActual:\n{actual'}"

    let remaining = MultiHashSet expected

    for item, range, _ in actual do
        if not (remaining.Remove((item, Some range)) || remaining.Remove((item, None))) then fail ()

    if not remaining.IsEmpty then fail ()
