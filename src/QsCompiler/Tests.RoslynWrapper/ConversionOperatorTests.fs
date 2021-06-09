namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module ConversionOperatorTests =
    [<Fact>]
    let ``conversion operator: implicit`` () =
        let m = 
            ``implicit operator`` "string" ``(`` (``type`` "C") ``)`` 
                [ ``public``; ``static`` ] 
                (``=>`` (``invoke`` ("value.ToString" |> ident) ``(`` [] ``)``))

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static implicit operator string(C value) => value.ToString();
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``conversion operator: implicit with forced static`` () =
        let m = 
            ``implicit operator`` "string" ``(`` (``type`` "C") ``)`` 
                [ ``public`` ] 
                (``=>`` (``invoke`` ("value.ToString" |> ident) ``(`` [] ``)``))

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static implicit operator string(C value) => value.ToString();
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``conversion operator: explicit`` () =
        let m = 
            ``explicit operator`` "string" ``(`` (``type`` "C") ``)`` 
                (``=>`` (``invoke`` (ident "value.ToString") ``(`` [] ``)``))

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static explicit operator string(C value) => value.ToString();
    }
}"
        are_equal expected actual



    [<Fact>]
    let ``conversion operator: explicit from-string`` () =
        let m = 
            ``explicit operator`` "C" ``(`` (``type`` "string") ``)`` 
                (``=>`` (``new`` (``type`` [ "C" ])  ``(`` [ ident "value"] ``)``))

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static explicit operator C(string value) => new C(value);
    }
}"
        are_equal expected actual


