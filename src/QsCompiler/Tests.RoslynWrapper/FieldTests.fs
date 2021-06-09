namespace Microsoft.Quantum.RoslynWrapper.Testing

open Xunit

open Microsoft.Quantum.RoslynWrapper

module FieldTests =
    [<Fact>]
    let ``field: uninitialized`` () =
        let m = 
            ``field`` "string" "m_Name" 
                [ ``private`` ] 
                None

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        private string m_Name;
    }
}"
        are_equal expected actual

    [<Fact>]
    let ``field: initialized`` () =
        let e = ``:=`` <| literal "John"
        let m = 
            ``field`` "string" "m_Name" 
                [ ``private`` ] 
                (Some e)

        let actual = to_class_members_code [m]
        let expected = @"namespace N
{
    using System;

    public class C
    {
        private string m_Name = ""John"";
    }
}"
        are_equal expected actual

