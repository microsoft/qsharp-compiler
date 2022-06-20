namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>object instantiation</code>
/// </summary>
[<AutoOpen>]
module ObjectCreation =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setArguments arguments (oce: ObjectCreationExpressionSyntax) =
        arguments
        |> Seq.map (SyntaxFactory.Argument)
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ArgumentList)
        |> oce.WithArgumentList

    let private setInitialization (elements: ExpressionSyntax list) (oce: ObjectCreationExpressionSyntax) =
        (SyntaxKind.CollectionInitializerExpression, elements |> SyntaxFactory.SeparatedList)
        |> SyntaxFactory.InitializerExpression
        |> oce.WithInitializer

    let ``new`` genericName ``(`` arguments ``)`` =
        genericName |> SyntaxFactory.ObjectCreationExpression |> setArguments arguments :> ExpressionSyntax


    let ``new init`` genericName ``(`` arguments ``)`` ``{`` elements ``}`` =
        genericName
        |> SyntaxFactory.ObjectCreationExpression
        |> setArguments arguments
        |> setInitialization elements
        :> ExpressionSyntax

    let ``new array`` arrayType arrayElements =
        let elems =
            arrayElements
            |> List.map (fun x -> x :> ExpressionSyntax)
            |> SyntaxFactory.SeparatedList
            |> (SyntaxFactory.InitializerExpression SyntaxKind.ArrayInitializerExpression).WithExpressions

        match arrayType with
        | None -> elems :> ExpressionSyntax
        | Some typeName ->
            let array = (``array type`` typeName None) :?> ArrayTypeSyntax
            (array, elems) |> SyntaxFactory.ArrayCreationExpression :> ExpressionSyntax

    let ``new array ranked`` arrayType arrayRanks =
        ``array type`` arrayType (Some arrayRanks) :?> ArrayTypeSyntax
        |> SyntaxFactory.ArrayCreationExpression
        :> ExpressionSyntax
