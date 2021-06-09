namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module ConstructorTests =    
    [<Fact>]
    let ``constructor: empty``() =
        let m = 
            ``constructor`` "C" ``(`` [] ``)`` 
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public C()
        {
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``constructor: with parameter``() =
        let m = 
            ``constructor`` "C" ``(`` [ ("thing", (``type`` "object")) ] ``)`` 
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public C(object thing)
        {
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``constructor: with parameter 2``() =
        let m = 
            ``constructor`` "C" ``(`` [ ("thing", (``type`` "object")); ("name", (``type`` "string")) ] ``)`` 
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public C(object thing, string name)
        {
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``constructor: calling base constructor``() =
        let m = 
            ``constructor`` "C" ``(`` [ ("thing", (``type`` "object")) ] ``)`` 
                ``:`` [ "thing" ]
                [``public``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public C(object thing) : base(thing)
        {
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``constructor: calling base constructor 2``() =
        let m = 
            ``constructor`` "C" ``(`` [ ("thing", (``type`` "object")); ("name", (``type`` "string")) ] ``)`` 
                ``:`` [ "thing"; "name" ]
                [``public``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public C(object thing, string name) : base(thing, name)
        {
        }
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``constructor: private``() =
        let m = 
            ``constructor`` "C" ``(`` [] ``)`` 
                ``:`` []
                [``private``]
                ``{``
                    []
                ``}``

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        private C()
        {
        }
    }
}"
        are_equal expected actual
