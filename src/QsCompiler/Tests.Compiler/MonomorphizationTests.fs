// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

open Xunit
open Xunit.Abstractions
open Microsoft.Quantum.QsCompiler
open System

type MonomorphizationTests (output:ITestOutputHelper) =
    //inherit CompilerTests(CompilerTests.Compile "TestCases" ["temporary-test-file.qs"], output)

    //member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
    //    let ns = "Microsoft.Quantum.Testing.Monomorphization" |> NonNullable<_>.New
    //    let name = name |> NonNullable<_>.New
    //    this.Verify (QsQualifiedName.New (ns, name), diag)
    //

    [<Fact>]
    member this.``Monomorphization`` () =
        let configuration = new CompilationLoader.Configuration (Monomorphization = true, MonomorphizationValidation = true)
        let loaded = new CompilationLoader(["TestCases/temporary-test-file.qs"], [], Nullable(configuration), null)

        Assert.True(loaded.Monomorphization = CompilationLoader.Status.Succeeded, sprintf "Monomorphization Failed")
        Assert.True(loaded.MonomorphizationValidation = CompilationLoader.Status.Succeeded, sprintf "Monomorphization Validation Failed")