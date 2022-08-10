// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.VisualStudio.LanguageServer.Protocol
open System.Collections.Generic
open System.Collections.Immutable
open System
open System.IO
open System.Linq
open Xunit

type WholeFileTests() =

    let compile file =
        let compilation = CompilerTests.Compile(Path.Join("TestCases", "WholeFileTests"), [ file ])
        compilation.Diagnostics()

    let verifyDiagnosticsOfSeverity (diag: IEnumerable<Diagnostic>) severity filename (expected: IEnumerable<_>) =
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
                "%A code mismatch for %s \nexpected: %s\ngot: %s\nmessages: \n\t%s"
                severity
                filename
                (String.Join(", ", expected))
                (String.Join(", ", got))
                (String.Join("\n\t", diag.Where(fun d -> d.Severity = severity).Select(fun d -> d.Message)))
        )

    member this.Verify(diag, filename, expected: IEnumerable<ErrorCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity diag (Nullable DiagnosticSeverity.Error) filename expected

    member this.Verify(diag, filename, expected: IEnumerable<WarningCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity diag (Nullable DiagnosticSeverity.Warning) filename expected

    member this.Verify(diag, filename, expected: IEnumerable<InformationCode>) =
        let expected = expected.Select int
        verifyDiagnosticsOfSeverity diag (Nullable DiagnosticSeverity.Information) filename expected

    member this.VerifyDiagnostics(diag, filename, expected: IEnumerable<DiagnosticItem>) =
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

        this.Verify(diag, filename, errs)
        this.Verify(diag, filename, wrns)
        this.Verify(diag, filename, infs)

        let other =
            expected
            |> Seq.choose (function
                | Warning _
                | Error _ -> None
                | item -> Some item)

        if other.Any() then NotImplementedException "unknown diagnostics item to verify" |> raise

    member this.Expect filename (diag: IEnumerable<DiagnosticItem>) =
        let actualDiag = compile filename
        this.VerifyDiagnostics(actualDiag, filename, diag)
