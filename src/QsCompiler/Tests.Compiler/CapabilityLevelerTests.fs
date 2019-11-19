// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.CapabilityLevelerTests

open System
open System.IO
open Microsoft.Quantum.QsCompiler.CompilationBuilder
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Targeting.Leveler
open Xunit


// Utilities for testing operation capability level setting and the corresponding infrastructure

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
                          |> Option.map (fun s -> s.Signature.Information.InferredInformation.RequiredCapabilityLevel)
                          |> Option.defaultValue CapabilityLevel.Unset
    tree |> Seq.tryPick (findOperation opName)
         |> Option.defaultWith (fun () -> failwithf "Operation %A not found" opName)
         |> getOperationBodyLevel
         

//////////////////////////////// tests //////////////////////////////////

[<Fact>]
let ``basic capability leveling`` () =
    let leveler = new TreeLeveler()

    let syntaxTree = buildSyntaxTreeFromFile "CapabilityLevels" |> Seq.map leveler.Transform

    let mLevel = getOperationLevel syntaxTree "M"
    Assert.Equal(CapabilityLevel.Minimal, mLevel)

    let hLevel = getOperationLevel syntaxTree "H"
    Assert.Equal(CapabilityLevel.Minimal, hLevel)

    let level1Level = getOperationLevel syntaxTree "Level1"
    Assert.Equal(CapabilityLevel.Minimal, level1Level)

    let level2Level = getOperationLevel syntaxTree "Level2"
    Assert.Equal(CapabilityLevel.Basic, level2Level)

    let level4Level = getOperationLevel syntaxTree "Level4"
    Assert.Equal(CapabilityLevel.Advanced, level4Level)

    let level5Level = getOperationLevel syntaxTree "Level3"
    Assert.Equal(CapabilityLevel.Medium, level5Level)

    ()