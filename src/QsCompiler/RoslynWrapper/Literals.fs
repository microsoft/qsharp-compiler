namespace Microsoft.Quantum.RoslynWrapper
[<AutoOpen>]
module Literals =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let ``true`` = SyntaxFactory.LiteralExpression SyntaxKind.TrueLiteralExpression
    let ``false`` = SyntaxFactory.LiteralExpression SyntaxKind.FalseLiteralExpression
    let ``null``  = SyntaxFactory.LiteralExpression SyntaxKind.NullLiteralExpression

    type Literal = Literal with
        static member ($) (_ : Literal, x : string)  = (SyntaxKind.StringLiteralExpression,    (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : char)    = (SyntaxKind.CharacterLiteralExpression, (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : decimal) = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : float)   = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) 
                                                       |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
                                                       |> fun x -> SyntaxFactory.CastExpression (ident "double", x) :> ExpressionSyntax
        static member ($) (_ : Literal, x : int)     = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : int64)   = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : float32) = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : uint32)  = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax
        static member ($) (_ : Literal, x : uint64)  = (SyntaxKind.NumericLiteralExpression,   (SyntaxFactory.Literal x)) |> SyntaxFactory.LiteralExpression :> ExpressionSyntax

    let inline literal s = Literal $ s

    let interpolated (s : string) = SyntaxFactory.ParseExpression ("$\"" + s + "\"")
