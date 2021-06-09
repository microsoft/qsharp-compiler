namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module CompilationUnitTests =
    [<Fact>]
    let ``compilation-unit : empty``() =
        let input = ``compilation unit`` [ ] [ ] [ ]
        let actual = generateCodeToString input
        let expected = @""
        are_equal expected actual
                 
    [<Fact>]
    let ``compilation-unit : single namespace``() =
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    [ ``using`` "System" ]
                    []
                ``}``
        let input = ``compilation unit`` [] [] [ n ]
        let actual = generateCodeToString input
        let expected = @"namespace Foo
{
    using System;
}"
        are_equal expected actual

    [<Fact>]
    let ``compilation-unit : single namespace with using``() =
        let usings = 
            [ 
                ``using`` "System.IO" 
                ``using`` "System.Linq" 
                ``using`` "System.Text" 
            ]
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    []
                    []
                ``}``
        let input = ``compilation unit`` [] usings [ n ]
        let actual = generateCodeToString input
        let expected = @"using System.IO;
using System.Linq;
using System.Text;

namespace Foo
{
}"
        are_equal expected actual
        
    [<Fact>]
    let ``compilation-unit : two namespaces``() =
        let usings = 
            [ 
                ``using`` "System.IO" 
                ``using`` "System.Linq" 
            ]
        let n1 = 
            ``namespace`` "Foo"
                ``{`` 
                    [ ``using`` "System" ]
                    []
                ``}``
                
        let n2 = 
            ``namespace`` "Bar"
                ``{`` 
                    [ ``using`` "System.Web" ]
                    []
                ``}``
        let input = ``compilation unit`` [] usings [ n1;n2 ]
        let actual = generateCodeToString input
        let expected = @"using System.IO;
using System.Linq;

namespace Foo
{
    using System;
}

namespace Bar
{
    using System.Web;
}"
        are_equal expected actual

    [<Fact>]
    let ``compilation-unit : attributes``() =
        let attributes = 
            [
                ``attribute`` (Some ``assembly``) (``ident`` "AssemblyTitleAttribute") [literal "MyAssembly"]
                ``attribute`` (Some ``assembly``) (``ident`` "CustomAttribute") []
                ``attribute`` (Some ``assembly``) (``ident`` "OtherCustomAttribute") [literal 3; literal 5]
            ]
        let usings = 
            [ 
                ``using`` "System.IO" 
                ``using`` "System.Reflection"  
            ]
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
        let input = ``compilation unit`` attributes usings [ n ]
        let actual = generateCodeToString input
        let expected = """using System.IO;
using System.Reflection;

[assembly: AssemblyTitleAttribute("MyAssembly")]
[assembly: CustomAttribute()]
[assembly: OtherCustomAttribute(3, 5)]
namespace Foo
{
    using System;
    using System.Collections;

    public class C
    {
    }
}"""
        are_equal expected actual


    [<Fact>]
    let ``compilation-unit : pragma``() =
        let n = 
            ``namespace`` "Foo"
                ``{`` 
                    [
                    ]
                    [
                    ]
                ``}``
        let input = ``compilation unit`` [] [] [ n ]
                    |> ``pragmaDisableWarning`` 1591
        let actual = generateCodeToString input
        let expected = @"#pragma warning disable 1591
namespace Foo
{
}"
        are_equal expected actual

