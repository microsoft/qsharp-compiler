// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.QirTests

open System.IO
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Xunit
open System.Reflection
open System.Text.RegularExpressions

let private GUID = new Regex(@"[({]?[a-fA-F0-9]{8}[-]?([a-fA-F0-9]{4}[-]?){3}[a-fA-F0-9]{12}[})]?", RegexOptions.IgnoreCase)

let private testOne expected args = 
    let result = Program.Main args
    Assert.Equal(expected, result)

let private clearOutput name =
    File.WriteAllText(name, "Test did not run to completion")

let private checkAltOutput name actualText =
    let expectedText = ("TestCases","QirTests",name) |> Path.Combine |> File.ReadAllText
    Assert.Contains(expectedText, GUID.Replace(actualText, "__GUID__"))

let private qirMultiTest target name snippets =
    clearOutput (name+".ll")
    [|
        "build"
        "-o"
        "outputFolder"
        "--proj"
        name
        "--build-exe"
        "--input"
        ("TestCases","QirTests",name+".qs") |> Path.Combine
        ("TestCases","QirTests","QirCore.qs") |> Path.Combine
        (if target then ("TestCases","QirTests","QirTarget.qs") |> Path.Combine else "")
        "--qir"
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        "--verbosity" 
        "Diagnostic"
    |]
    |> testOne ReturnCode.SUCCESS
    let fullText = (name+".ll") |> File.ReadAllText
    snippets |> List.map (fun s -> checkAltOutput (s+".ll") fullText)

let private qirTest target name =
    qirMultiTest target name [name]


[<Fact>]
let ``QIR using`` () =
    qirMultiTest true "TestUsing" ["TestUsing1"; "TestUsing2"]

[<Fact>]
let ``QIR inlined call`` () =
    qirTest true "TestInline"

[<Fact>]
let ``QIR access counts`` () =
    qirTest false "TestAccessCounts"
    
[<Fact>]
let ``QIR array loop`` () =
    qirTest false "TestArrayLoop"

[<Fact>]
let ``QIR nested for loop`` () =
    qirTest false "TestForLoop"

[<Fact>]
let ``QIR caching of values`` () =
    qirTest true "TestCaching"

[<Fact>]
let ``QIR array update`` () =
    qirMultiTest false "TestArrayUpdate" ["TestArrayUpdate1"; "TestArrayUpdate2"; "TestArrayUpdate3"; "TestArrayUpdate4"; "TestArrayUpdate5"]
    
[<Fact>]
let ``QIR tuple deconstructing`` () =
    qirTest false "TestDeconstruct"
    
[<Fact>]
let ``QIR UDT constructor`` () =
    qirTest false "TestUdt"
    
[<Fact>]
let ``QIR UDT construction`` () =
    qirTest false "TestUdtConstruction"
    
[<Fact>]
let ``QIR UDT accessor`` () =
    qirTest false "TestUdtAccessor"
   
[<Fact>]
let ``QIR UDT update`` () =
    qirMultiTest false "TestUdtUpdate" ["TestUdtUpdate1"; "TestUdtUpdate2"]

[<Fact>]
let ``QIR UDT argument`` () =
    qirTest false "TestUdtArgument"

[<Fact>]
let ``QIR callable values`` () =
    qirMultiTest false "TestLocalCallables" ["TestLocalCallables1"; "TestLocalCallables2"]
    
[<Fact>]
let ``QIR operation argument`` () =
    qirTest true "TestOpArgument"

[<Fact>]
let ``QIR operation call`` () =
    qirMultiTest false "TestOpCall" ["TestOpCall1"; "TestOpCall2"]

[<Fact>]
let ``QIR while loop`` () =
    qirTest false "TestWhile"
    
[<Fact>]
let ``QIR repeat loop`` () =
    qirTest true "TestRepeat"
    
[<Fact>]
let ``QIR integers`` () =
    qirTest false "TestIntegers"

[<Fact>]
let ``QIR doubles`` () =
    qirTest false "TestDoubles"

[<Fact>]
let ``QIR bools`` () =
    qirTest false "TestBools"

[<Fact>]
let ``QIR bigints`` () =
    qirTest false "TestBigInts"

[<Fact>]
let ``QIR controlled partial applications`` () =
    qirMultiTest true "TestControlled" ["TestControlled1"; "TestControlled2"; "TestControlled3"]

[<Fact>]
let ``QIR entry points`` () =
    qirTest false "TestEntryPoint"

[<Fact>]
let ``QIR partial applications`` () =
    qirMultiTest true "TestPartials" ["TestPartials1"; "TestPartials2"; "TestPartials3"; "TestPartials4"]

[<Fact>]
let ``QIR declarations`` () =
    qirMultiTest false "TestDeclarations" ["TestDeclarations1"; "TestDeclarations2"; "TestDeclarations3"; "TestDeclarations4"; "TestDeclarations5"; "TestDeclarations6"]

[<Fact>]
let ``QIR functors`` () =
    qirTest true "TestFunctors"

[<Fact>]
let ``QIR paulis`` () =
    qirTest false "TestPaulis"

[<Fact>]
let ``QIR results`` () =
    qirTest false "TestResults"

[<Fact>]
let ``QIR ranges`` () =
    qirTest false "TestRange"

[<Fact>]
let ``QIR strings`` () =
    qirTest false "TestStrings"

[<Fact>]
let ``QIR scoping`` () =
    qirTest false "TestScoping"

[<Fact>]
let ``QIR conditionals`` () =
    qirTest false "TestConditional"

[<Fact>]
let ``QIR expressions`` () =
    qirTest false "TestExpressions"
