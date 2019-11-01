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
open Microsoft.Quantum.QsCompiler.Targeting.Leveler
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput
open Xunit

// Utilities for testing operation capability level setting and the corresponding infrastructure

let out = File.CreateText("C:\\Users\\ageller\\Source\\Repos\\debug.log")
System.Console.SetOut(out)

let private buildSyntaxTree path code =
    let fileId = new Uri(path) 
    let compilationUnit = new CompilationUnitManager(fun ex -> failwith ex.Message) 
    let file = CompilationUnitManager.InitializeFileManager(fileId, code)
    compilationUnit.AddOrUpdateSourceFileAsync file |> ignore  // spawns a task that modifies the current compilation
    compilationUnit.GetSyntaxTree()   // will wait for any current tasks to finish

let private buildSyntaxTreeFromFile fileName =
    let path = Path.Combine(Path.GetFullPath ".", "TestCases", fileName + ".qs") 
    path |> File.ReadAllText |> buildSyntaxTree path

//let private 

let private getOperationLevel tree opName =
    let matchOperation opName (e : QsNamespaceElement) =
        match e with
        | QsCallable c when c.FullName.Name.Value = opName ->
            Some c
        | _ -> None
    let findOperation opName (ns : QsNamespace) =
        ns.Elements |> Seq.tryPick (matchOperation opName)
    let getOperationBodyLevel (c : QsCallable) =
        c.Specializations |> Seq.tryFind (fun s -> s.Kind = QsSpecializationKind.QsBody)
                          |> Option.map (fun s -> s.RequiredCapability)
                          |> Option.defaultValue CapabilityLevel.Unset
    tree |> Seq.tryPick (findOperation opName)
         |> Option.defaultWith (fun () -> failwithf "Operation %A not found" opName)
         |> getOperationBodyLevel
         

//////////////////////////////// tests //////////////////////////////////

[<Fact>]
let ``capability leveling tests`` () =
    let leveler = new TreeLeveler()
    let syntaxTree = buildSyntaxTreeFromFile "CapabilityLevels" |> Seq.map leveler.Transform

    Assert.Equal(CapabilityLevel.Minimal, getOperationLevel syntaxTree "M")
    ()