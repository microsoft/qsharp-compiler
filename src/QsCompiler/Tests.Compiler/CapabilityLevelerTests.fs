// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityLevelerTests

open System
open System.Collections.Immutable
open System.IO
open System.Linq
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit

// Utilities for testing operation capability level setting and the corresponding infrastructure

let private buildSyntaxTree path code =
    let fileId = new Uri(path) 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    let mutable syntaxTree = compilationUnit.GetSyntaxTree()   // will wait for any current tasks to finish
    //FunctorGeneration.GenerateFunctorSpecializations(syntaxTree, &syntaxTree) |> ignore
    syntaxTree

let private buildSyntaxTreeFromFile fileName =
    let path = Path.Combine(Path.GetFullPath ".", "TestFiles", fileName) 
    path |> File.ReadAllText |> buildSyntaxTree path

//let private 

let private getOperationLevel tree opName =
    let matchOperation opName (e : QsNamespaceElement) =
        match e with
        | QsCallable c when c.FullName.Name.Value = opName ->
            Some c
        | _ -> None
    let findOperation (ns : QsNamespace) opName =
        ns.Elements |> Seq.tryPick (matchOperation opName)
    tree |> Seq.tryPick (findOperation opName)
         

//////////////////////////////// tests //////////////////////////////////

