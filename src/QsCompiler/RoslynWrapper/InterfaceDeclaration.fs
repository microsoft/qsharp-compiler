namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for an <code>interface</code>
/// </summary>
[<AutoOpen>]
module InterfaceDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setModifiers modifiers (cd: InterfaceDeclarationSyntax) =
        modifiers |> Seq.map SyntaxFactory.Token |> SyntaxFactory.TokenList |> cd.WithModifiers

    let private setMembers members (cd: InterfaceDeclarationSyntax) = cd.AddMembers(members |> Seq.toArray)

    let private setBases bases (cd: InterfaceDeclarationSyntax) =
        if bases |> Seq.isEmpty then
            cd
        else
            bases
            |> Seq.map (ident >> SyntaxFactory.SimpleBaseType >> (fun b -> b :> BaseTypeSyntax))
            |> (SyntaxFactory.SeparatedList >> SyntaxFactory.BaseList)
            |> cd.WithBaseList

    let private setTypeParameters typeParameters (cd: InterfaceDeclarationSyntax) =
        if typeParameters |> Seq.isEmpty then
            cd
        else
            typeParameters
            |> Seq.map (SyntaxFactory.Identifier >> SyntaxFactory.TypeParameter)
            |> (SyntaxFactory.SeparatedList >> SyntaxFactory.TypeParameterList)
            |> cd.WithTypeParameterList

    let ``interface`` interfaceName ``<<`` typeParameters ``>>`` ``:`` baseInterfaces modifiers ``{`` members ``}`` =
        interfaceName
        |> (SyntaxFactory.Identifier >> SyntaxFactory.InterfaceDeclaration)
        |> setTypeParameters typeParameters
        |> setBases baseInterfaces
        |> setModifiers modifiers
        |> setMembers members
