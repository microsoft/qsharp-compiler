namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module NamespaceTests = 

    [<Fact>]
    let ``namespace: empty``() =
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    []
                    []
                ``}``
        let actual = to_namespace_code n
        let expected = @"namespace Foo
{
}"
        are_equal expected actual

    [<Fact>]
    let ``namespace: with usings``() =
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    [ ``using`` "System.Collections" ]
                    []
                ``}``
        let actual = to_namespace_code n
        let expected = @"namespace Foo
{
    using System.Collections;
}"
        are_equal expected actual

    [<Fact>]
    let ``namespace: with usings and classes``() =
        let c =
            ``class`` "C" ``<<`` [] ``>>``
                ``:`` None ``,`` []
                [``public``]
                ``{``
                    []
                ``}``
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    [ 
                        ``using`` "System"
                        ``using`` "System.Collections"
                    ]
                    [ c ]
                ``}``
        let actual = to_namespace_code n
        let expected = @"namespace Foo
{
    using System;
    using System.Collections;

    public class C
    {
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``namespace: type aliases``() =
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    [ 
                        ``using`` "System" 
                        ``using`` "System.Collections" 
                        ``alias`` "Foo" "Int"
                    ]
                    []
                ``}``
        let actual = to_namespace_code n
        let expected = @"namespace Foo
{
    using System;
    using System.Collections;
    using Foo = Int;
}"
        are_equal expected actual


