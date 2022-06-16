namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Generate if statements
/// </summary>
[<AutoOpen>]
module IfStatement =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private createBlock (stmts: StatementSyntax list) =
        stmts |> (Seq.toArray >> SyntaxFactory.Block)

    let rec private buildElifClauses (clauses: (ExpressionSyntax * BlockSyntax) list) (final: ElseClauseSyntax option) =
        match clauses with
        | [] -> final
        | (expr, block) :: tail ->
            SyntaxFactory.ElseClause(
                SyntaxFactory.Token SyntaxKind.ElseKeyword,
                match final with
                | None -> SyntaxFactory.IfStatement(expr, block)
                | Some inner -> SyntaxFactory.IfStatement(expr, block, inner)
            )
            |> Some
            |> buildElifClauses tail

    // if (condition) { thenStatements } else { elseStatements }
    //      -> elseStatements is Option: if None, else block is skipped
    let ``if`` ``(`` condition ``)`` thenStatements elseClause =
        let ss = (condition, (createBlock thenStatements)) |> SyntaxFactory.IfStatement

        match elseClause with
        | Some clause -> ss.WithElse clause
        | None -> ss
        :> StatementSyntax

    let ``elif`` elifsStatements elseClause =
        let elifs = elifsStatements |> List.map (fun (e, b) -> e, createBlock b)
        buildElifClauses (elifs |> List.rev) elseClause

    let ``else`` (elseStatements: StatementSyntax list) =
        elseStatements |> createBlock |> SyntaxFactory.ElseClause

    let ``?`` condition (whenTrue, whenFalse) =
        SyntaxFactory.ConditionalExpression(condition, whenTrue, whenFalse) :> ExpressionSyntax
