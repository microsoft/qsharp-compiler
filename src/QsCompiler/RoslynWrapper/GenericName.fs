namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>generic type</code> name
/// </summary>
[<AutoOpen>]
module GenericName =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setArrayRank (ranks: ExpressionSyntax list option) (at: ArrayTypeSyntax) =
        let ranks =
            match ranks with
            | None -> [ (SyntaxFactory.OmittedArraySizeExpression()) :> ExpressionSyntax ]
            | Some r -> r

        [ ranks |> SyntaxFactory.SeparatedList |> SyntaxFactory.ArrayRankSpecifier ]
        |> SyntaxFactory.List
        |> at.WithRankSpecifiers

    let private setTypeArgumentList typeArguments (gn: GenericNameSyntax) =
        typeArguments
        |> Seq.map (ident >> (fun t -> t :> TypeSyntax))
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.TypeArgumentList)
        |> gn.WithTypeArgumentList

    let private simpleType parts =
        let rec toQualifiedNameImpl =
            function
            | [] -> failwith "cannot get qualified name of empty list"
            | [ n ] -> n |> ident :> NameSyntax
            | [ p1; p2 ] -> (p2, p1) |> mapTuple2 ident |> SyntaxFactory.QualifiedName :> NameSyntax
            | p1 :: rest -> (toQualifiedNameImpl rest, p1 |> ident) |> SyntaxFactory.QualifiedName :> NameSyntax

        parts |> List.rev |> toQualifiedNameImpl :> TypeSyntax

    type SimpleType = SimpleType
        with
            static member ($)(_: SimpleType, x: string) = simpleType [ x ]
            static member ($)(_: SimpleType, xs: string list) = simpleType xs

    let inline ``type`` s = SimpleType $ s
    let object = SyntaxKind.ObjectKeyword |> SyntaxFactory.Token |> SyntaxFactory.PredefinedType

    let ``generic`` name ``<<`` typeArguments ``>>`` =
        name |> (SyntaxFactory.Identifier >> SyntaxFactory.GenericName) |> setTypeArgumentList typeArguments

    let ``array type`` arrayType arrayRanks =
        arrayType |> ident |> SyntaxFactory.ArrayType |> setArrayRank arrayRanks :> TypeSyntax

    let ``type name`` (typeSyntax: TypeSyntax) = typeSyntax.ToFullString()
