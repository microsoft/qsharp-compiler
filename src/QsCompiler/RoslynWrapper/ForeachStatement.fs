namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Generate foreach statements
/// </summary>
[<AutoOpen>]
module ForeachStatement =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private createBlock (stmts: StatementSyntax list) =
        stmts |> (Seq.toArray >> SyntaxFactory.Block) |> ``#line hidden`` // this one is a bit hacky, but makes debuggin experience better for q# generated code.

    let private createVar = "var" |> ident :> TypeSyntax

    // if (condition) { thenStatements } else { elseStatements }
    //      -> elseStatements is Option: if None, else block is skipped
    let ``foreach`` ``(`` (variable: string) ``in`` expression ``)`` statements =
        (createVar, variable, expression, createBlock statements) |> SyntaxFactory.ForEachStatement :> StatementSyntax
