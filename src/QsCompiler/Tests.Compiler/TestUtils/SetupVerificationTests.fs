// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Testing

// TODO: Remove this.
type CompilerTests(compilation) =
    let diagnostics = Diagnostics.byDeclaration compilation

    member _.Diagnostics name = diagnostics[name]

    member _.VerifyDiagnostics(name, expected) =
        Diagnostics.assertMatches (Seq.map (fun e -> e, None) expected) diagnostics[name]

    static member Compile(srcFolder, fileNames, ?references, ?capability, ?isExecutable) =
        let references = defaultArg references []
        let output = if Option.contains true isExecutable then TestUtils.Exe else TestUtils.Library
        TestUtils.buildFiles srcFolder fileNames references capability output
