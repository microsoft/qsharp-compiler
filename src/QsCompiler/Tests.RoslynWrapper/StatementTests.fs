namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module StatementTests = 
    open Microsoft.CodeAnalysis.CSharp.Syntax

    [<Fact>]
    let ``expression: new``() =
        let t = generic "List" ``<<`` [ "int" ] ``>>``
        let s = ``new`` t ``(`` [ ] ``)``
        let m = return_from_arrow_method (``type name`` t) s
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal List<int> Host() => new List<int>();
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: new() with args``() =
        let s = ``new`` (``type`` ["System"; "String" ]) ``(`` [ literal "A" ] ``)``
        let m = return_from_arrow_method "String" s
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal String Host() => new System.String(""A"");
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: new with initialization``() =
        let elems = 
            [
                ``literal`` 1
                ``literal`` 2
                ``literal`` 3
            ] 
        let t = generic "List" ``<<`` [ "int" ] ``>>``
        let s = ``new init`` t ``(`` [ ] ``)`` ``{`` elems ``}``
        let m = return_from_arrow_method (``type name`` t) s
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal List<int> Host() => new List<int>()
        {1, 2, 3};
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: tuple``() =
        
        let elems = 
            [
                ``ident`` "a"
                ``ident`` "b"
                ``ident`` "c"
            ] 
        let s1 = ``var`` "x" (``:=`` (``tuple``  elems) ) 
        let m = host_in_method "void" [s1]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var x = (a, b, c);
        }
    }
}"
        are_equal expected actual

        
    [<Fact>]
    let ``expression: tuple deconstruct``() =
        let innerTuple = 
            ``tuple`` 
                [
                    (literal "c")
                    (literal 1)
                ]
        let outerTuple = 
            ``tuple``
                [
                    (``ident`` "a") :> ExpressionSyntax 
                    (``ident`` "b") :> ExpressionSyntax 
                    innerTuple
                ]

        let decl =
            [
                ("int", "x")        |> declare
                ("string", "y")     |> declare
                ``deconstruct``
                    [
                        ("string", "alpha")      |> declare
                        ("int", "beta")      |> declare
                    ]
            ]
        let s1 = (``deconstruct`` decl) <-- outerTuple  |> statement
        let m = host_in_method "void" [s1]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            (int x, string y, (string alpha, int beta)) = (a, b, (""c"", 1));
        }
    }
}"
        are_equal expected actual
    [<Fact>]
    let ``statement: empty return``() =
        let s = ``return`` None
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            return;
        }
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``statement: return value``() =
        let s = ``return`` (Some <| literal 42)
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            return 42;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: assignment``() =
        let target = ident "a"
        let source = literal 42
        let s = statement (target <-- source)
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            a = 42;
        }
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``statement: empty throw``() =
        let s = ``throw`` None
        let m = host_in_method "void" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            throw;
        }
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``statement: throw exception``() =
        let newException = ``new`` (``type`` ["System"; "Exception"]) ``(`` [] ``)``
        let s = ``throw`` <| Some newException
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            throw new System.Exception();
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: cast``() =
        let expr = ``cast`` "float" (literal 42)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = (float)42;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: as``() =
        let expr = ``as`` "float" (ident "b")
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b as float;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: |~>``() =
        let expr = (ident "b") |~> "float" 
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b as float;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: is``() =
        let expr = ``is`` "float" (ident "b")
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b is float;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: ==``() =
        let expr = (ident "b") .==. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b == 12;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: &&``() =
        let expr = (ident "b") .&&. (ident "c")
        let s = ((ident "a") <-- expr) |> statement
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b && c;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: ||``() =
        let expr = (ident "b") .||. (ident "c")
        let s = ((ident "a") <-- expr) |> statement
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b || c;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: !=``() =
        let expr = (ident "b") .!=. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b != 12;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: >=``() =
        let expr = (ident "b") .>=. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b >= 12;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: >``() =
        let expr = (ident "b") .>. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b > 12;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: <=``() =
        let expr = (ident "b") .<=. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b <= 12;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: <``() =
        let expr = (ident "b") .<. (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b < 12;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: ^``() =
        let expr = (ident "b") <^> (literal 12)
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b ^ 12;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: ??``() =
        let expr = (ident "b") <??> ``false``
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = b ?? false;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: paranthesize``() =
        let expr = ``((`` ((ident "b") <^> (literal 12)) ``))``
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = (b ^ 12);
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: !``() =
        let expr = ! ((ident "b") <^> (literal 12))
        let s = ((ident "a") <-- expr) |> statement 
        let m = host_in_method "int" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            a = !(b ^ 12);
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: member access``() =
        let ma = (ident "System") <|.|> (ident "Console") <.> (ident "WriteLine", [ literal "Hello, World!" ])
        let m = host_in_method "int" [ statement ma ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            System.Console.WriteLine(""Hello, World!"");
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: member access safe``() =
        let ma = (ident "System") <|?.|> (ident "Console") <?.> (ident "WriteLine", [ literal "Hello, World!" ])
        let m = host_in_method "int" [ statement ma ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            System?.Console?.WriteLine(""Hello, World!"");
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: async - await``() =
        let ma = (ident "System") <|.|> (ident "Console") <.> (ident "WriteLine", [ literal "Hello, World!" ])
        let s = ``await`` ma |> statement
        let m =
            ``method`` "int" "Host" ``<<`` [] ``>>`` ``(`` [] ``)``
                [``protected``; ``internal``; ``async`` ]
                ``{``
                    [s]
                ``}``
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal async int Host()
        {
            await System.Console.WriteLine(""Hello, World!"");
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: member access generic``() =
        let gen1 = generic "Task" ``<<`` [ "int" ] ``>>``
        let gen2 = generic "Run" ``<<`` [ "string" ] ``>>``
        let ma = (ident "System") <|.|> gen1 <.> (gen2, [ ``ident`` "a" ])
        let m = host_in_method "int" [ statement ma ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal int Host()
        {
            System.Task<int>.Run<string>(a);
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: literals``() =
        let ss = 
            [
                (ident "a") <-- (literal "Hello World")
                (ident "a") <-- (literal 'c')
                (ident "a") <-- (literal 42)
                (ident "a") <-- (literal 3.14159)
                (ident "a") <-- (literal 1.0)
                (ident "a") <-- (literal 100M)
                (ident "a") <-- (literal 2147483647u)
                (ident "a") <-- (literal 2147483647L)
                (ident "a") <-- (literal 9223372036854775807UL)
                (ident "a") <-- (literal 1.0f)
                (ident "a") <-- (interpolated "Test {a}")
                (ident "a") <-- ``true``
                (ident "a") <-- ``false``
                (ident "a") <-- ``null``
            ]
            |> Seq.map statement

        let m = host_in_method "void" ss
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            a = ""Hello World"";
            a = 'c';
            a = 42;
            a = (double)3.14159;
            a = (double)1;
            a = 100M;
            a = 2147483647U;
            a = 2147483647L;
            a = 9223372036854775807UL;
            a = 1F;
            a = $""Test {a}"";
            a = true;
            a = false;
            a = null;
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: single param lambda``() =
        let expr = ``as`` "float" (ident "b")
        let s = ``_ =>`` "b" expr 
        let m = return_from_arrow_method "Func<int, float>" s
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal Func<int, float> Host() => b => b as float;
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``expression: multi param lambda``() =
        let expr = ``as`` "float" (ident "b")
        let s = ``() =>`` ["a"; "b"] expr 
        let m = return_from_arrow_method "Func<int, float>" s
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal Func<int, float> Host() => (a, b) => b as float;
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: numerics``() =
        let ss = 
            [
                (ident "a") <-- ((literal 1) <+> (literal 2))
                (ident "b") <-- ((literal 1) <-> (literal 3))
                (ident "c") <-- (``-`` ((literal 1) <*> (literal 4)))
                (ident "d") <-- ((literal 1) </> (``-`` (literal 5)))
                (ident "e") <-- ((literal 1) <%> (literal 6))
            ]
            |> Seq.map statement

        let m = host_in_method "void" ss
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            a = 1 + 2;
            b = 1 - 3;
            c = -(1 * 4);
            d = 1 / -(5);
            e = 1 % 6;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: if then``() =
        let condition = (ident "b") .==. (literal 12)
        let thens = 
            [
                (ident "a") <-- ((literal 1) <+> (literal 2))
            ]
            |> List.map statement
        let stmt = ``if`` ``(`` condition ``)`` thens None
        let m = host_in_method "void" [ stmt ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            if (b == 12)
            {
                a = 1 + 2;
            }
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: if then else``() =
        let condition = (ident "b") .==. (literal 12)
        let thens = 
            [
                (ident "a") <-- ((literal 1) <+> (literal 2))
            ]
            |> List.map statement
        let elses = 
            [
                (ident "x") <-- ((literal 1) <-> (literal 3))
                (ident "y") <-- ((``-`` (literal 1)) <*> (literal 4))
            ]
            |> List.map statement
        let stmt = ``if`` ``(`` condition ``)`` thens (Some (``else`` elses))
        let m = host_in_method "void" [ stmt ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            if (b == 12)
            {
                a = 1 + 2;
            }
            else
            {
                x = 1 - 3;
                y = -(1) * 4;
            }
        }
    }
}"
        are_equal expected actual


    [<Fact>]
    let ``expression: if then elif else``() =
        let condition x = (ident "b") .==. (literal x)
        let thens x = 
            [ (ident "a") <-- ((literal 1) <+> (literal x)) ]
            |> List.map statement
        let (condition, thens), elifs =
            (condition 1, thens 1), [2..3] |> List.map (fun x -> condition x, thens x)
        let elses = 
            [
                (ident "x") <-- ((literal 1) <-> (literal 3))
                (ident "y") <-- ((``-`` (literal 1)) <*> (literal 4))
            ]
            |> List.map statement
        let stmt = ``if`` ``(`` condition ``)`` thens (``elif`` elifs (Some (``else`` elses)))
        let m = host_in_method "void" [ stmt ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            if (b == 1)
            {
                a = 1 + 1;
            }
            else if (b == 2)
            {
                a = 1 + 2;
            }
            else if (b == 3)
            {
                a = 1 + 3;
            }
            else
            {
                x = 1 - 3;
                y = -(1) * 4;
            }
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: if then else using elif``() =
        let condition x = (ident "b") .==. (literal x)
        let thens x = 
            [ (ident "a") <-- ((literal 1) <+> (literal x)) ]
            |> List.map statement
        let (c1, t1) = (condition 1, thens 1)
        let (c2, t2) = (condition 2, thens 2)
        let elses = 
            [
                (ident "x") <-- ((literal 1) <-> (literal 3))
                (ident "y") <-- ((``-`` (literal 1)) <*> (literal 4))
            ]
            |> List.map statement

        // ``if`` ``(`` condition ``)`` thens (``elif`` elifs (Some (``else`` elses)))
        let stmt1 = ``if`` ``(`` c1 ``)`` t1 (``elif`` [] None)
        let stmt2 = ``if`` ``(`` c2 ``)`` t2 (``elif`` [] (Some (``else`` elses)))
        let m = host_in_method "void" [ stmt1; stmt2 ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            if (b == 1)
            {
                a = 1 + 1;
            }

            if (b == 2)
            {
                a = 1 + 2;
            }
            else
            {
                x = 1 - 3;
                y = -(1) * 4;
            }
        }
    }
}"
        are_equal expected actual

        
    [<Fact>]
    let ``expression: cond ? true : false``() =
        let t = (literal "t")
        let f = (literal "f")
        let cond = ``true``
        let s = ``return`` (Some (``?`` cond (t, f)))
        let m = host_in_method "string" [s]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal string Host()
        {
            return true ? ""t"" : ""f"";
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: foreach``() =
        let elems = 
            [
                ``ident`` "a"
                ``ident`` "b"
                ``ident`` "c"
            ]
        let expr = (``new array`` (Some "Test") elems)
        let body = 
            [
                (``var`` "x") (``:=`` <| ((literal 1) <-> (literal 3)))
                ((ident "System") <|.|> (ident "Console") <.> (ident "WriteLine", [ ``ident`` "x" ])) |> statement
                ``return`` None
            ]
        let stmt = ``foreach`` ``(`` "i" ``in`` expr  ``)`` body
        let m = host_in_method "void" [ stmt ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            foreach (var i in new Test[] { a, b, c })
#line hidden
            {
                var x = 1 - 3;
                System.Console.WriteLine(x);
                return;
            }
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``expression: while``() =
        let condition = (ident "b") .==. (literal 12)
        let body = 
            [
                (ident "x") <-- ((literal 1) <-> (literal 3))           |> statement
                (ident "y") <-- ((``-`` (literal 1)) <*> (literal 4))   |> statement
                ``break``
            ]            
        let stmt = ``while`` ``(`` condition ``)`` body
        let m = host_in_method "void" [ stmt ]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            while (b == 12)
            {
                x = 1 - 3;
                y = -(1) * 4;
                break;
            }
        }
    }
}"
        are_equal expected actual