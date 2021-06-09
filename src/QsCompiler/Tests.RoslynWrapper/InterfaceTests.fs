namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module InterfaceTests =
    [<Fact>]
    let ``interface: empty``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    public interface I
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: generic``() =
        let i =
            ``interface`` "I" ``<<`` [ "T" ] ``>>``
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    public interface I<T>
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: generic 2``() =
        let i =
            ``interface`` "I" ``<<`` [ "R"; "S" ] ``>>``
                ``:`` []
                [``public``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    public interface I<R, S>
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: base interfaces``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` [ "IEnumerable"; "ISerializable" ]
                [``public``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    public interface I : IEnumerable, ISerializable
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: private``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``private``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    private interface I
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: static``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``static``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    static interface I
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: internal``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``internal``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    internal interface I
    {
    }
}"
        are_equal expected actual
        
    [<Fact>]
    let ``interface: partial``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``partial``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    partial interface I
    {
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``interface: private static partial``() =
        let i =
            ``interface`` "I" ``<<`` [] ``>>``
                ``:`` []
                [``private``; ``static``; ``partial``]
                ``{``
                    []
                ``}``
        let actual = to_namespace_member_code i
        let expected = @"namespace N
{
    using System;

    private static partial interface I
    {
    }
}"
        are_equal expected actual

