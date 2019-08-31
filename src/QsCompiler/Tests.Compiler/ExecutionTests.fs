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


type FunctorAutoGenTests (output:ITestOutputHelper) =

    //inherit CompilerTests("TestCases", ["General.qs"; "FunctorGeneration.qs"], output)
    //
    //member private this.Expect name (diag : IEnumerable<DiagnosticItem>) = 
    //    let ns = "Microsoft.Quantum.Testing.FunctorGeneration" |> NonNullable<_>.New
    //    let name = name |> NonNullable<_>.New
    //    this.Verify (QsQualifiedName.New (ns, name), diag)


    [<Fact>]
    member this.``Specialization Generation for Conjugations`` () = 
        let expectedBody = "
            U1
            V1
            U3
            V3
            Core3
            Adjoint V3
            Adjoint U3
            Core1
            U2
            V2
            Core2
            Adjoint V2
            Adjoint U2
            U3
            V3
            Adjoint Core3
            Adjoint V3
            Adjoint U3
            Adjoint V1
            Adjoint U1        
        "
        let expectedAdjoint = "
            U1
            V1
            U3
            V3
            Core3
            Adjoint V3
            Adjoint U3
            U2
            V2
            Adjoint Core2
            Adjoint V2
            Adjoint U2
            Adjoint Core1
            U3
            V3
            Adjoint Core3
            Adjoint V3
            Adjoint U3
            Adjoint V1
            Adjoint U1        
        "
        let expectedControlled = "
            U1
            V1
            U3
            V3
            Core3
            Adjoint V3
            Adjoint U3
            Controlled Core1
            U2
            V2
            Controlled Core2
            Adjoint V2
            Adjoint U2
            U3
            V3
            Adjoint Core3
            Adjoint V3
            Adjoint U3
            Adjoint V1
            Adjoint U1        
        "
        let expectedControlledAdjoint = "
            U1
            V1
            U3
            V3
            Core3
            Adjoint V3
            Adjoint U3
            U2
            V2
            Controlled Adjoint Core2
            Adjoint V2
            Adjoint U2
            Controlled Adjoint Core1
            U3
            V3
            Adjoint Core3
            Adjoint V3
            Adjoint U3
            Adjoint V1
            Adjoint U1        
        "
        ()