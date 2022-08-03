// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.RegexTests

open Xunit
open Microsoft.Quantum.QsCompiler.CommandLineCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Testing.TestUtils
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace


[<Fact>]
let ``Valid array bracket matching`` () =
    [
        "[]"
        "[ some text ]"
        "[ a;
           b
           c ]"
        "[[]]"
        "[a [], b sf; [a ; b c;] ]"
        "[a [], b [sf; [a ; b c;]; 4]]"
    ]
    |> List.iter (fun str -> FormatCompilation.WithinArrayBrackets.Match str |> verifyMatch str)

[<Fact>]
let ``Invalid array bracket matching`` () =
    [ "["; "]"; "some text" ]
    |> List.iter (fun str -> FormatCompilation.WithinArrayBrackets.Match str |> verifyNoMatch str)

[<Fact>]
let ``Array bracket matching within text`` () =
    [
        ("some text [] more text", [ "[]" ])
        ("[ []", [ "[]" ])
        ("[[], []", [ "[]"; "[]" ])
        ("[[]]", [ "[[]]" ])
        ("[a [], b sf; [a ; b c;] ]", [ "[a [], b sf; [a ; b c;] ]" ])
        ("one [1;2] two [1], []", [ "[1;2]"; "[1]"; "[]" ])
        ("a[1;2]-aeg\[[],[1;2]]", [ "[1;2]"; "[[],[1;2]]" ])
        ("[a [], b [sf; [a ; b c;]; 4]", [ "[]"; "[sf; [a ; b c;]; 4]" ])
        ("[a [], b sf; [a ; b c;]; 4]", [ "[a [], b sf; [a ; b c;]; 4]" ])
        ("[a [], b [sf; [a ; b c;]; 4", [ "[]"; "[a ; b c;]" ])
    ]
    |> List.iter (fun (str, exp) -> FormatCompilation.WithinArrayBrackets.Matches str |> verifyMatches exp)

[<Fact>]
let ``Replace array item delimeters`` () =
    [
        ("some text", "some text")
        ("some text [1;2;] more text", "some text [1,2,] more text")
        ("[ [;]", "[ [,]")
        ("[[1;2]; []", "[[1,2]; []")
        ("[a [], b sf; [a ; b c;] ]", "[a [], b sf, [a , b c,] ]")
        ("one [1;2] two [1], []", "one [1,2] two [1], []")
        ("a[1;2]-aeg\[[],[1;2]]", "a[1,2]-aeg\[[],[1,2]]")
        ("[a [1;]; b [sf; [a ; b c;]; 4]", "[a [1,]; b [sf, [a , b c,], 4]")
        ("[a [1;]; b sf; [a ; b c;]; 4]", "[a [1,], b sf, [a , b c,], 4]")
        ("[a [1;]; b [sf; [a ; b c;]; 4", "[a [1,]; b [sf; [a , b c,]; 4")
    ]
    |> List.iter (fun (str, exp) -> FormatCompilation.UpdateArrayLiterals str |> fun got -> Assert.Equal(exp, got))


[<Fact>]
let ``Strip unique variable name resolution`` () =
    let nameResolution = UniqueVariableNames()

    // name wrapping is added and stripped without verifying the validity of the variable name
    let origNames =
        [
            "var1"
            "__var2__"
            "3"
            "1+5"
            "some name" // the matching will fail (only) if there is a linebreak
            "'TName"
        ]

    for var in origNames do
        Assert.Equal(var, UniqueVariableNames.StripUniqueName var)

    for var in origNames do
        let unique = nameResolution.SharedState.GenerateUniqueName var
        let twiceWrapped = nameResolution.SharedState.GenerateUniqueName unique
        Assert.Equal(unique, UniqueVariableNames.StripUniqueName twiceWrapped)

    for var in origNames do
        let unique = nameResolution.SharedState.GenerateUniqueName var
        let twiceWrapped = nameResolution.SharedState.GenerateUniqueName unique
        let unique = UniqueVariableNames.StripUniqueName twiceWrapped
        Assert.Equal(var, UniqueVariableNames.StripUniqueName unique)

    for var in origNames do
        let unique = nameResolution.SharedState.GenerateUniqueName var
        Assert.Equal(var, UniqueVariableNames.StripUniqueName unique)
