namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module TryCatchTests = 
    [<Fact>]
    let ``expression: try catch``() =
        let t = 
            ``try`` 
                [ 
                    statement (ident "a" <-- literal 42)
                ]
                [
                    ``catch`` (Some ("Exception", "e")) [ ]
                ]
                None
        let m = host_in_method "void" [t]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            try
            {
                a = 42;
            }
            catch (Exception e)
            {
            }
        }
    }
}"
        are_equal expected actual

        
    [<Fact>]
    let ``expression: try finally``() =
        let a = 
            ``var`` "a" (``:=`` <| literal "") 
        let t = 
            ``try`` 
                [ 
                    statement (ident "a" <-- literal 42)
                ]
                [
                ]
                (Some (``finally`` 
                    [
                        statement (``invoke`` (``ident`` "Apply") ``(`` [``ident`` "a"] ``)``)
                    ]))
        let r = ``return`` (Some (``ident`` "a"))
        let m = host_in_method "string" [a;t;r]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal string Host()
        {
            var a = """";
            try
            {
                a = 42;
            }
            finally
            {
                Apply(a);
            }

            return a;
        }
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``expression: try catch finally``() =
        let a = 
            ``var`` "a" (``:=`` <| literal "") 
        let t = 
            ``try`` 
                [ 
                    statement (ident "a" <-- literal "foo")
                ]
                [
                    ``catch`` (Some ("Exception1", "e1"))
                        [
                            statement (ident "a" <-- literal "bar")
                        ]
                    ``catch`` (Some ("Exception2", "e2")) [ ]
                    ``catch`` None 
                        [
                            statement (ident "a" <-- literal "other")
                        ]

                ]
                (Some (``finally`` 
                    [
                        statement (``invoke`` (``ident`` "Apply") ``(`` [``ident`` "a"] ``)``)
                    ]))
        let m = host_in_method "void" [a;t]
        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        protected internal void Host()
        {
            var a = """";
            try
            {
                a = ""foo"";
            }
            catch (Exception1 e1)
            {
                a = ""bar"";
            }
            catch (Exception2 e2)
            {
            }
            catch
            {
                a = ""other"";
            }
            finally
            {
                Apply(a);
            }
        }
    }
}"
        are_equal expected actual

