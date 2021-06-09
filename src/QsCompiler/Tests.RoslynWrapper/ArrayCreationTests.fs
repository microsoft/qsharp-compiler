namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module ArrayCreationTests =
    open Microsoft.CodeAnalysis.CSharp.Syntax

    [<Fact>]
    let ``array: new empty array``() =
        let s = ``var`` "a" (``:=`` (``new array`` (Some "int") [ ]))
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var a = new int[] { };
        }
    }
}"
        are_equal expected actual

        
    [<Fact>]
    let ``array: new initialized array with ids``() =
        
        let elems = 
            [
                ``ident`` "a"
                ``ident`` "b"
                ``ident`` "c"
            ]
        let s1 = ``var`` "x" (``:=`` (``new array`` (Some "Test") elems))
        let s2 = ``var`` "y" (``:=`` (``item`` (``ident`` "x") [ ``literal`` 1 ]))
        let m = host_in_method "void" [s1;s2]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var x = new Test[] { a, b, c };
            var y = x[1];
        }
    }
}"
        are_equal expected actual

        
    [<Fact>]
    let ``array: new initialized array with constants``() =
        
        let elems = 
            [
                ``literal`` 1
                ``literal`` 2
                ``literal`` 3
            ]
        let s = ``var`` "a" (``:=`` (``new array`` (Some "int") elems))
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var a = new int[] { 1, 2, 3 };
        }
    }
}"
        are_equal expected actual

    
    [<Fact>]
    let ``array: new ranked array``() =
        let s = ``var`` "a" (``:=`` (``new array ranked`` "int" [(literal 5)]))
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var a = new int[5];
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``array: new multi-dimensional array``() =
        let s = ``var`` "a" (``:=`` (``new array ranked`` "int" [(literal 5);(literal 8)]))
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var a = new int[5, 8];
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``array: typed array initialized with constants -short-``() =
        
        let elems = 
            [
                ``literal`` 1
                ``literal`` 2
                ``literal`` 3
            ]
        let s = ``typed array`` "int" "a" (Some (``:=`` (``new array`` None elems)))
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            int[] a = { 1, 2, 3 };
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``array: typed array -short-``() =
        let s = ``typed array`` "int" "a" None
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            int[] a;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``array: new array as argument``() =
        
        let elems = 
            [
                ``literal`` 1
                ``literal`` 2
                ``literal`` 3
            ]
        let arg1 = (``new array`` (Some "int") elems)
        let arg2 = (``ident`` "p2") :> ExpressionSyntax
        let s =  statement (``invoke`` (``ident`` "Apply") ``(`` [arg1;arg2] ``)``)
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            Apply(new int[] { 1, 2, 3 }, p2);
        }
    }
}"
        are_equal expected actual