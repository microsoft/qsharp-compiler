namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>class constructor</code>
/// </summary>
[<AutoOpen>]
module ConstructorDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setModifiers modifiers (cd: ConstructorDeclarationSyntax) =
        modifiers |> Seq.map SyntaxFactory.Token |> SyntaxFactory.TokenList |> cd.WithModifiers

    let private setParameterList constructorParams (cd: ConstructorDeclarationSyntax) =
        constructorParams
        |> Seq.map (fun (paramName, paramType) -> param paramName ``of`` paramType)
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ParameterList)
        |> cd.WithParameterList

    let private setBodyBlock bodyBlockStatements (cd: ConstructorDeclarationSyntax) =
        bodyBlockStatements |> (Seq.toArray >> SyntaxFactory.Block) |> cd.WithBody

    let private setInitializer baseConstructorParameters (cd: ConstructorDeclarationSyntax) =
        if baseConstructorParameters |> Seq.isEmpty then
            cd
        else
            baseConstructorParameters
            |> Seq.map (ident >> SyntaxFactory.Argument)
            |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ArgumentList)
            |> (fun args -> SyntaxFactory.ConstructorInitializer(SyntaxKind.BaseConstructorInitializer, args))
            |> cd.WithInitializer

    let ``constructor``
        className
        ``(``
        parameters
        ``)``
        ``:``
        baseConstructorParameters
        modifiers
        ``{``
        bodyBlockStatements
        ``}``
        =
        className
        |> (SyntaxFactory.Identifier >> SyntaxFactory.ConstructorDeclaration)
        |> setInitializer baseConstructorParameters
        |> setParameterList parameters
        |> setModifiers modifiers
        |> setBodyBlock bodyBlockStatements
