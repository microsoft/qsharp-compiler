namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182" // Unused parameters

/// <summary>
/// Use this module to specify the syntax for a <code>method invocations</code>
/// </summary>
[<AutoOpen>]
module Invocation =
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax

    let private setArguments (methodArguments: ArgumentSyntax seq) (ie: InvocationExpressionSyntax) =
        methodArguments
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.ArgumentList)
        |> ie.WithArgumentList

    let ``invoke`` m ``(`` args ``)`` =
        m |> SyntaxFactory.InvocationExpression |> setArguments (args |> Seq.map SyntaxFactory.Argument)
        :> ExpressionSyntax
