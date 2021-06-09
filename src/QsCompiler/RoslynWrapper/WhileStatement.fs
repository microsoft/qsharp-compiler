namespace Microsoft.Quantum.RoslynWrapper

/// <summary>
/// Generate while statements
/// </summary>
#nowarn "1182"    // Unused parameters
[<AutoOpen>]
module WhileStatement =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private createBlock (stmts: StatementSyntax list) =
        stmts
        |> (Seq.toArray >> SyntaxFactory.Block)   
        
        
    // while (condition) { statements } 
    let ``while`` ``(`` condition ``)`` statements =
        (condition, createBlock statements)
        |> SyntaxFactory.WhileStatement
        :> StatementSyntax