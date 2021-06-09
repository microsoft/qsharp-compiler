namespace Microsoft.Quantum.RoslynWrapper

/// <summary>
/// Use this module to specify the syntax for a <code>class or interface property</code>
/// </summary>
#nowarn "1182"    // Unused parameters
[<AutoOpen>]
module PropertyDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setModifiers modifiers (pd : PropertyDeclarationSyntax) =
        modifiers
        |> Seq.map SyntaxFactory.Token
        |> SyntaxFactory.TokenList
        |> pd.WithModifiers

    let private setBodyBlock bodyBlockStatements (ad : AccessorDeclarationSyntax) =    
        match bodyBlockStatements with
        | Some body ->
            body
            |> (Seq.toArray >> SyntaxFactory.Block)
            |> ad.WithBody
        | None ->
            SyntaxKind.SemicolonToken
            |> SyntaxFactory.Token
            |> ad.WithSemicolonToken

    let private setArrowBody body (ad : AccessorDeclarationSyntax) =    
        body
        |> ad.WithExpressionBody
        |> (fun ad ->
            SyntaxKind.SemicolonToken
            |> SyntaxFactory.Token
            |> ad.WithSemicolonToken)

    let private setGetAccessor bodyBlockStatements (pd : PropertyDeclarationSyntax) =
        SyntaxKind.GetAccessorDeclaration
        |> SyntaxFactory.AccessorDeclaration
        |> setBodyBlock bodyBlockStatements
        |> (fun ad -> pd.AddAccessorListAccessors ad)

    let private setArrowGetAccessor body (pd : PropertyDeclarationSyntax) =
        body
        |> pd.WithExpressionBody
        |> (fun pd ->
            SyntaxKind.SemicolonToken
            |> SyntaxFactory.Token
            |> pd.WithSemicolonToken)

    let private setSetAccessor bodyBlockStatements (pd : PropertyDeclarationSyntax) =
        SyntaxKind.SetAccessorDeclaration
        |> SyntaxFactory.AccessorDeclaration
        |> setBodyBlock bodyBlockStatements
        |> (fun ad -> pd.AddAccessorListAccessors ad)

    let private setArrowSetAccessor body (pd : PropertyDeclarationSyntax) =
        SyntaxKind.SetAccessorDeclaration
        |> SyntaxFactory.AccessorDeclaration
        |> setArrowBody body
        |> (fun ad -> pd.AddAccessorListAccessors ad)

        

    let private createPropertyDeclaration propertyType propertyName modifiers getBodyBlockStatements setBodyBlockStatements =
        (propertyType |> ident, propertyName |> SyntaxFactory.Identifier)
        |> SyntaxFactory.PropertyDeclaration
        |> setModifiers modifiers
        |> setGetAccessor getBodyBlockStatements
        |> setSetAccessor setBodyBlockStatements

    let private createGetPropertyDeclaration propertyType propertyName modifiers getBodyBlockStatements =
        (propertyType |> ident, propertyName |> SyntaxFactory.Identifier)
        |> SyntaxFactory.PropertyDeclaration
        |> setModifiers modifiers
        |> setGetAccessor getBodyBlockStatements

    let private createArrowGetPropertyDeclaration propertyType propertyName modifiers getBody =
        (propertyType |> ident, propertyName |> SyntaxFactory.Identifier)
        |> SyntaxFactory.PropertyDeclaration
        |> setModifiers modifiers
        |> setArrowGetAccessor getBody
        
    let ``property`` propertyType propertyName modifiers
            ``get``       
                    getBodyBlockStatements
            ``set``
                    setBodyBlockStatements
            =
        createPropertyDeclaration propertyType propertyName modifiers (Some getBodyBlockStatements) (Some setBodyBlockStatements)
        
    let ``property-get`` propertyType propertyName modifiers         
            ``get``       
                    getBodyBlockStatements
            =
        createGetPropertyDeclaration propertyType propertyName modifiers (Some getBodyBlockStatements)

    let ``property-arrow_get`` propertyType propertyName modifiers         
            ``get``       
                    getBody 
            =
        createArrowGetPropertyDeclaration propertyType propertyName modifiers getBody

    let ``prop`` propertyType propertyName modifiers =
        createPropertyDeclaration propertyType propertyName modifiers None None

    let ``propg`` propertyType propertyName modifiers =
        createGetPropertyDeclaration propertyType propertyName modifiers None
