namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Generate while statements
/// </summary>
[<AutoOpen>]
module WhileStatement =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private createBlock (stmts: StatementSyntax list) =
        stmts |> (Seq.toArray >> SyntaxFactory.Block)


    // while (condition) { statements }
    let ``while`` ``(`` condition ``)`` statements =
        (condition, createBlock statements) |> SyntaxFactory.WhileStatement :> StatementSyntax
