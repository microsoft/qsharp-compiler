namespace Microsoft.Quantum.RoslynWrapper

/// <summary>
/// Use this module to specify the syntax for a <code>compilation unit</code>
/// </summary>
[<AutoOpen>]
module CompilationUnit =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let ``assembly``= 
        SyntaxFactory.Token (SyntaxKind.AssemblyKeyword)
        |> SyntaxFactory.AttributeTargetSpecifier

    let ``attribute`` targetOpt (attributeName : NameSyntax) (attributeArguments : ExpressionSyntax seq) = 
        let attribute = 
            // let identifier = ``ident`` att
            let args = 
                attributeArguments
                |> Seq.map SyntaxFactory.AttributeArgument
                |> (Seq.toArray >> SyntaxFactory.SeparatedList)
                |> SyntaxFactory.AttributeArgumentList
            SyntaxFactory.Attribute (attributeName, args)
        match targetOpt with 
        | None          -> SyntaxFactory.AttributeList([|attribute|] |> SyntaxFactory.SeparatedList)
        | Some target   -> SyntaxFactory.AttributeList(target, [|attribute|] |> SyntaxFactory.SeparatedList)

    let private addUsings usings (cu : CompilationUnitSyntax) =
        usings
        |> (Seq.toArray >> SyntaxFactory.List)
        |> cu.WithUsings

    let private addMembers members (cu : CompilationUnitSyntax) =
        members
        |> (Seq.toArray >> SyntaxFactory.List)
        |> cu.WithMembers

    let private addAttributes att (cu : CompilationUnitSyntax) = 
        att
        |> (Seq.toArray >> SyntaxFactory.List)
        |> cu.WithAttributeLists

    /// This function creates a 'compilation unit' given a sequence of members.
    let ``compilation unit`` att usings members =
        SyntaxFactory.CompilationUnit()
        |> addAttributes att
        |> addUsings usings
        |> addMembers members
        |> (fun cu -> cu.NormalizeWhitespace())

    let ``pragmaDisableWarning`` warning (cu : CompilationUnitSyntax) =
        let pr = SyntaxFactory.PreprocessingMessage (sprintf "#pragma warning disable %d" warning)
        cu.WithLeadingTrivia [| 
            yield pr; yield SyntaxFactory.CarriageReturnLineFeed;
            for trivia in cu.GetLeadingTrivia() do yield trivia 
        |]

    let ``with leading comments`` comments (cu : CompilationUnitSyntax) =
        cu.WithLeadingTrivia [| 
            for comment in comments |> Seq.map SyntaxFactory.Comment do 
                yield comment; 
                yield SyntaxFactory.CarriageReturnLineFeed 
            for trivia in cu.GetLeadingTrivia() do yield trivia             
        |] 
