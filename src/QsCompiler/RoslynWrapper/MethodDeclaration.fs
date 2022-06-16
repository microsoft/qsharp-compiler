namespace Microsoft.Quantum.RoslynWrapper

open Microsoft.CodeAnalysis

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>class or interface method</code>
/// </summary>
[<AutoOpen>]
module MethodDeclaration =
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setModifiers modifiers (md: MethodDeclarationSyntax) =
        modifiers |> Seq.map SyntaxFactory.Token |> SyntaxFactory.TokenList |> md.WithModifiers

    let private setParameterList (parameters: ParameterSyntax list) (md: MethodDeclarationSyntax) =
        parameters |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ParameterList) |> md.WithParameterList

    let private setExpressionBody methodBody (md: MethodDeclarationSyntax) =
        methodBody |> Option.fold (fun (_md: MethodDeclarationSyntax) _mb -> _md.WithExpressionBody _mb) md

    let private setBodyBlock bodyBlockStatements (md: MethodDeclarationSyntax) =
        bodyBlockStatements |> (Seq.toArray >> SyntaxFactory.Block) |> md.WithBody

    let private setTypeParameters typeParameters (md: MethodDeclarationSyntax) =
        if typeParameters |> Seq.isEmpty then
            md
        else
            typeParameters
            |> Seq.map (SyntaxFactory.Identifier >> SyntaxFactory.TypeParameter)
            |> (SyntaxFactory.SeparatedList >> SyntaxFactory.TypeParameterList)
            |> md.WithTypeParameterList

    let private addClosingSemicolon (md: MethodDeclarationSyntax) =
        SyntaxKind.SemicolonToken |> SyntaxFactory.Token |> md.WithSemicolonToken

    let ``arrow_method``
        methodType
        methodName
        ``<<``
        methodTypeParameters
        ``>>``
        ``(``
        methodParams
        ``)``
        modifiers
        methodBodyExpression
        =
        (methodType |> ident, methodName |> SyntaxFactory.Identifier)
        |> SyntaxFactory.MethodDeclaration
        |> setTypeParameters methodTypeParameters
        |> setModifiers modifiers
        |> setParameterList methodParams
        |> setExpressionBody methodBodyExpression
        |> addClosingSemicolon

    let ``method``
        methodType
        methodName
        ``<<``
        methodTypeParameters
        ``>>``
        ``(``
        methodParams
        ``)``
        modifiers
        ``{``
        bodyBlockStatements
        ``}``
        =
        (methodType |> ident, methodName |> SyntaxFactory.Identifier)
        |> SyntaxFactory.MethodDeclaration
        |> setTypeParameters methodTypeParameters
        |> setModifiers modifiers
        |> setParameterList methodParams
        |> setBodyBlock bodyBlockStatements

    let ``with trivia`` (trivia: SyntaxTrivia) (method: MethodDeclarationSyntax) =
        let bodyWithTrivia =
            method.Body.WithOpenBraceToken(
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(trivia),
                    SyntaxKind.OpenBraceToken,
                    SyntaxFactory.TriviaList()
                )
            )

        method.WithBody bodyWithTrivia
