namespace Microsoft.Quantum.RoslynWrapper

[<AutoOpen>]
module EqualityOperators =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setParameterList parameters (co : OperatorDeclarationSyntax) =
        parameters
        |> Seq.map (fun (paramName, paramType) -> ``param`` paramName ``of`` paramType)
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ParameterList)
        |> co.WithParameterList

    let private setModifiers modifiers (co : OperatorDeclarationSyntax) =
        modifiers
        |> Set.ofSeq
        |> (fun s -> s.Add ``static``)
        |> Seq.map SyntaxFactory.Token
        |> SyntaxFactory.TokenList
        |> co.WithModifiers

    let private setExpressionBody body (co : OperatorDeclarationSyntax) =
        co.WithExpressionBody body

    let private addClosingSemicolon (co : OperatorDeclarationSyntax) =
        SyntaxKind.SemicolonToken |> SyntaxFactory.Token
        |> co.WithSemicolonToken

    let ``operator ==`` (left, right, vtype) initializer =
        (SyntaxKind.BoolKeyword |> (SyntaxFactory.Token >> SyntaxFactory.PredefinedType), SyntaxKind.EqualsEqualsToken |> SyntaxFactory.Token)
        |> SyntaxFactory.OperatorDeclaration
        |> setParameterList [ (left, vtype); (right, vtype)]
        |> setModifiers [``public``; ``static``]
        |> setExpressionBody initializer
        |> addClosingSemicolon

    let ``operator !=`` (left, right, vtype) initializer =
        (SyntaxKind.BoolKeyword |> (SyntaxFactory.Token >> SyntaxFactory.PredefinedType), SyntaxKind.ExclamationEqualsToken |> SyntaxFactory.Token)
        |> SyntaxFactory.OperatorDeclaration
        |> setParameterList [ (left, vtype); (right, vtype)]
        |> setModifiers [``public``; ``static``]
        |> setExpressionBody initializer
        |> addClosingSemicolon
