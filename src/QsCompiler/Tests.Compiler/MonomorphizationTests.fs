// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxExtensions
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open System

type MonomorphizationTests (output:ITestOutputHelper) =
    //inherit CompilerTests(CompilerTests.Compile "TestCases" ["temporary-test-file.qs"], output)

    let builtTree =
        let configuration = new CompilationLoader.Configuration (Monomorphization = true, MonomorphizationValidation = true)
        let loaded = new CompilationLoader(["test.qs"], [], Nullable(configuration), null)
        if loaded.Monomorphization = CompilationLoader.Status.Failed then null
        else loaded.GeneratedSyntaxTree

    //member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
    //    let ns = "Microsoft.Quantum.Testing.Monomorphization" |> NonNullable<_>.New
    //    let name = name |> NonNullable<_>.New
    //    this.Verify (QsQualifiedName.New (ns, name), diag)
    //
    //[<Fact>]
    //member this.``Monomorphization`` () =
    //    this.Expect "" []