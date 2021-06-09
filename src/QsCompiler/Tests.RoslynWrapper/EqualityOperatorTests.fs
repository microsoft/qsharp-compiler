namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module EqualityOperatorTests =
    [<Fact>]
    let ``equality operator: ==`` () =
        let m = 
            ``operator ==`` ("left", "right", ``type`` "string")
                (``=>`` ``true``)

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static bool operator ==(string left, string right) => true;
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``equality operator: !=`` () =
        let m = 
            ``operator !=`` ("left", "right", ``type`` "string")
                (``=>`` ``true``)

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        public static bool operator !=(string left, string right) => true;
    }
}"
        are_equal expected actual
