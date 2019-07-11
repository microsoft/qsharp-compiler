// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.Testing.RegexTests

open TestUtils
open Xunit
open Microsoft.Quantum.QsCompiler.CommandLineCompiler


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
    |> List.iter (fun str -> FormatCompilation.WithinArrayBrackets.Match str |> VerifyMatch str)

[<Fact>]
let ``Invalid array bracket matching`` () =
    [
        "["
        "]"
        "some text"
    ]
    |> List.iter (fun str -> FormatCompilation.WithinArrayBrackets.Match str |> VerifyNoMatch str)

[<Fact>]
let ``Array bracket matching within text`` () =
    [
        ("some text [] more text"       , ["[]"                         ])
        ("[ []"                         , ["[]"                         ])
        ("[[], []"                      , ["[]"; "[]"                   ])
        ("[[]]"                         , ["[[]]"                       ])
        ("[a [], b sf; [a ; b c;] ]"    , ["[a [], b sf; [a ; b c;] ]"  ])
        ("one [1;2] two [1], []"        , ["[1;2]"; "[1]"; "[]"         ])
        ("a[1;2]-aeg\[[],[1;2]]"        , ["[1;2]"; "[[],[1;2]]"        ])
        ("[a [], b [sf; [a ; b c;]; 4]" , ["[]"; "[sf; [a ; b c;]; 4]"  ])
        ("[a [], b sf; [a ; b c;]; 4]"  , ["[a [], b sf; [a ; b c;]; 4]"])
        ("[a [], b [sf; [a ; b c;]; 4"  , ["[]"; "[a ; b c;]"           ])
    ]
    |> List.iter (fun (str,exp) -> FormatCompilation.WithinArrayBrackets.Matches str |> VerifyMatches exp)

[<Fact>]
let ``Replace array item delimeters`` () =
    [
        ("some text"                      , "some text"                     )
        ("some text [1;2;] more text"     , "some text [1,2,] more text"    )
        ("[ [;]"                          , "[ [,]"                         )
        ("[[1;2]; []"                     , "[[1,2]; []"                    )
        ("[a [], b sf; [a ; b c;] ]"      , "[a [], b sf, [a , b c,] ]"     )
        ("one [1;2] two [1], []"          , "one [1,2] two [1], []"         )
        ("a[1;2]-aeg\[[],[1;2]]"          , "a[1,2]-aeg\[[],[1,2]]"         )
        ("[a [1;]; b [sf; [a ; b c;]; 4]" , "[a [1,]; b [sf, [a , b c,], 4]")
        ("[a [1;]; b sf; [a ; b c;]; 4]"  , "[a [1,], b sf, [a , b c,], 4]" )
        ("[a [1;]; b [sf; [a ; b c;]; 4"  , "[a [1,]; b [sf; [a , b c,]; 4" )
    ]
    |> List.iter (fun (str,exp) -> FormatCompilation.UpdateArrayLiterals str |> fun got -> Assert.Equal(exp, got))


