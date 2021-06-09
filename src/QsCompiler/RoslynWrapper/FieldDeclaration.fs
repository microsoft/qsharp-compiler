namespace Microsoft.Quantum.RoslynWrapper

/// <summary>
/// Use this module to specify the syntax for a <code>field</code>
/// </summary>
[<AutoOpen>]
module FieldDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setVariableInitializer initializer (vd : VariableDeclaratorSyntax) =
        initializer
        |> Option.fold (fun (_vd : VariableDeclaratorSyntax) _in -> _vd.WithInitializer _in) vd

    let private setFieldVariable fieldName fieldInitializer (vd : VariableDeclarationSyntax) =
        [ fieldName ]
        |> Seq.map (SyntaxFactory.Identifier >> SyntaxFactory.VariableDeclarator >> setVariableInitializer fieldInitializer)
        |> SyntaxFactory.SeparatedList
        |> vd.WithVariables

    let private setModifiers modifiers (fd : FieldDeclarationSyntax)  =
        modifiers
        |> Seq.map SyntaxFactory.Token
        |> SyntaxFactory.TokenList
        |> fd.WithModifiers

    let ``field`` fieldType fieldName modifiers fieldInitializer =
        fieldType
        |> (ident >> SyntaxFactory.VariableDeclaration)
        |> setFieldVariable fieldName fieldInitializer
        |> SyntaxFactory.FieldDeclaration
        |> setModifiers modifiers
