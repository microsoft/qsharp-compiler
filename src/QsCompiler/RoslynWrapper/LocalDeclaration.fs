namespace Microsoft.Quantum.RoslynWrapper

/// Use this module to specify the syntax for a local variable
[<AutoOpen>]
module LocalDeclaration =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setVariableInitializer initializer (vd : VariableDeclaratorSyntax) =
        initializer
        |> Option.fold (fun (_vd : VariableDeclaratorSyntax) _in -> _vd.WithInitializer _in) vd

    let private setVariableDeclarator localName localInitializer (vd : VariableDeclarationSyntax) =
        [ localName ]
        |> Seq.map (SyntaxFactory.Identifier >> SyntaxFactory.VariableDeclarator >> setVariableInitializer localInitializer)
        |> SyntaxFactory.SeparatedList
        |> vd.WithVariables
    
    let ``typed var`` localType localName localInitializer =
        localType
        |> (ident >> SyntaxFactory.VariableDeclaration)
        |> setVariableDeclarator localName localInitializer
        |> SyntaxFactory.LocalDeclarationStatement

    let ``typed array`` localType localName localInitializer =        
        ``array type`` localType None 
        |> SyntaxFactory.VariableDeclaration
        |> setVariableDeclarator localName localInitializer
        |> SyntaxFactory.LocalDeclarationStatement

    let ``var`` localName localInitializer =
        ``typed var`` "var" localName (Some localInitializer)
         :> StatementSyntax
    