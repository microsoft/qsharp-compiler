namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Generate while statements
/// </summary>
[<AutoOpen>]
module TryStatement =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private createBlock (stmts: StatementSyntax list) =
        stmts |> (Seq.toArray >> SyntaxFactory.Block)

    let private addDeclaration (excpt: (string * string) option) (ccs: CatchClauseSyntax) =
        match excpt with
        | Some (t, id) ->
            (t |> ``type``, id |> SyntaxFactory.Identifier)
            |> SyntaxFactory.CatchDeclaration
            |> ccs.WithDeclaration
        | None -> ccs

    let private addCatchBlock stmts (clause: CatchClauseSyntax) =
        stmts |> createBlock |> clause.WithBlock

    let private addFinallyBlock stmts (ts: TryStatementSyntax) = stmts |> createBlock |> ts.WithBlock

    let private addCatches (catches: CatchClauseSyntax list) (ts: TryStatementSyntax) =
        ts.WithCatches(catches |> Seq.toArray |> SyntaxFactory.List)

    let private addFinally (fin: FinallyClauseSyntax option) (ts: TryStatementSyntax) =
        match fin with
        | Some fin -> fin |> ts.WithFinally
        | None -> ts

    let ``catch`` excpt stmts =
        SyntaxFactory.CatchClause() |> addDeclaration excpt |> addCatchBlock stmts

    let ``finally`` stmts =
        stmts |> createBlock |> SyntaxFactory.FinallyClause

    // try { } (catch { catches }) (finally { finallyBlock } )
    let ``try`` stmts catches finClause =
        SyntaxFactory.TryStatement() |> addFinallyBlock stmts |> addCatches catches |> addFinally finClause
        :> StatementSyntax
