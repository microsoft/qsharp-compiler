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

open System
open System.IO
open System.Linq
open System.Text
open System.Text.RegularExpressions
open Microsoft.Quantum.QsCompiler

type ExecutionTests (output:ITestOutputHelper) =

    let WS = new Regex(@"\s+"); 
    let stripWS str = WS.Replace (str, ""); 
    let AssertEqual expected got = 
        Assert.True (
            stripWS expected = stripWS got, 
            sprintf "expected: \n%s\ngot: \n%s" expected got)

    let ExecuteOnQuantumSimulator cName = 
        let exitCode, ex = ref -101, ref null
        let out, err = ref (new StringBuilder()), ref (new StringBuilder())
        let exe = File.ReadAllLines("ExecutionTarget.txt").Single()
        let args = sprintf "%s %s.%s" exe "Microsoft.Quantum.Testing.ExecutionTests" cName
        let ranToEnd = ProcessRunner.Run ("dotnet", args, out, err, exitCode, ex, timeout = 10000)
        Assert.True(ranToEnd)
        Assert.Null(!ex)
        Assert.Equal(0, !exitCode)
        (!out).ToString(), (!err).ToString()

    let ExecuteAndCompareOutput cName expectedOutput = 
        let out, err = ExecuteOnQuantumSimulator cName
        AssertEqual String.Empty err
        AssertEqual expectedOutput out


    [<Fact>]
    member this.``Specialization Generation for Conjugations`` () = 

        ExecuteAndCompareOutput 
            "ConjugationsInBody" "
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

        ExecuteAndCompareOutput 
            "ConjugationsInAdjoint" "
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

        ExecuteAndCompareOutput 
            "ConjugationsInControlled" "
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

        ExecuteAndCompareOutput 
            "ConjugationsInControlledAdjoint" "
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

