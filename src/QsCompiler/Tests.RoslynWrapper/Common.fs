namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit
open System.Text.RegularExpressions

open Microsoft.Quantum.RoslynWrapper

[<AutoOpen>]
module internal Common = 
    let private normalizeNewLine s = Regex.Replace(s, "(?<!\r)\n", "\r\n")
    
    let are_equal (expected : string) (actual : string) = 
        (expected, actual) |> (mapTuple2 normalizeNewLine >> Assert.Equal)

    let to_namespace_code n = 
        let cu = ``compilation unit`` [] [] [ n ]
        generateCodeToString cu
        
    let to_namespace_member_code c = 
        let ns = 
            ``namespace`` "N" 
                ``{``
                    [ ``using`` "System" ]
                    [ c ]
                ``}``
        in to_namespace_code ns
    
    let to_class_members_code ms = 
        let c = 
            ``class`` "C" ``<<`` [] ``>>``
                ``:`` None ``,`` []
                [``public``]
                ``{``
                    ms
                ``}``
         in to_namespace_member_code c

    let to_interface_members_code ms = 
        let c = 
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``public``]
                ``{``
                    ms
                ``}``
         in to_namespace_member_code c

    let host_in_method t ss = 
        ``method`` t "Host" ``<<`` [] ``>>`` ``(`` [] ``)``
            [``protected``; ``internal`` ]
            ``{``
                ss
            ``}``

    let return_from_arrow_method t s = 
        ``arrow_method`` t "Host" ``<<`` [] ``>>`` ``(`` [] ``)``
            [``protected``; ``internal`` ]
            (Some <| ``=>`` s)

