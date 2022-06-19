namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for <code>explicit</code> and <code>implicit</code> conversion operators
/// </summary>
[<AutoOpen>]
module Conversion =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setParameterList parameters (co: ConversionOperatorDeclarationSyntax) =
        parameters
        |> Seq.map (fun (paramName, paramType) -> param paramName ``of`` paramType)
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ParameterList)
        |> co.WithParameterList

    let private setModifiers modifiers (co: ConversionOperatorDeclarationSyntax) =
        modifiers
        |> Set.ofSeq
        |> (fun s -> s.Add ``static``)
        |> Seq.map SyntaxFactory.Token
        |> SyntaxFactory.TokenList
        |> co.WithModifiers

    let private setExpressionBody body (co: ConversionOperatorDeclarationSyntax) = co.WithExpressionBody body

    let private addClosingSemicolon (co: ConversionOperatorDeclarationSyntax) =
        SyntaxKind.SemicolonToken |> SyntaxFactory.Token |> co.WithSemicolonToken

    let ``explicit operator`` target ``(`` source ``)`` initializer =
        (SyntaxKind.ExplicitKeyword |> SyntaxFactory.Token, target |> ident)
        |> SyntaxFactory.ConversionOperatorDeclaration
        |> setParameterList [ ("value", source) ]
        |> setModifiers [ ``public``; ``static`` ]
        |> setExpressionBody initializer
        |> addClosingSemicolon

    let ``implicit operator`` target ``(`` source ``)`` modifiers initializer =
        (SyntaxKind.ImplicitKeyword |> SyntaxFactory.Token, target |> ident)
        |> SyntaxFactory.ConversionOperatorDeclaration
        |> setParameterList [ ("value", source) ]
        |> setModifiers modifiers
        |> setExpressionBody initializer
        |> addClosingSemicolon
