namespace Microsoft.Quantum.RoslynWrapper

#nowarn "1182"    // Unused parameters
[<AutoOpen>]
module Expressions = 
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax
    
    // target = source
    let (<--) target source =
         SyntaxFactory.AssignmentExpression (SyntaxKind.SimpleAssignmentExpression, target, source)

    // left += right
    let (<+=>) left right =         
        SyntaxFactory.AssignmentExpression(SyntaxKind.AddAssignmentExpression, left, right)
        |> SyntaxFactory.ExpressionStatement

    // left -= right
    let (<-=>) left right =         
        SyntaxFactory.AssignmentExpression(SyntaxKind.SubtractAssignmentExpression, left, right)
        |> SyntaxFactory.ExpressionStatement

    // (targetType) expression        
    let ``cast`` targetType expression = 
        SyntaxFactory.CastExpression (ident targetType, expression) :> ExpressionSyntax

    // expression as targetType
    let ``as`` targetType expression = 
        SyntaxFactory.BinaryExpression (SyntaxKind.AsExpression, expression, ident targetType)

    /// alias for the ``as`` function
    let (|~>) expression targetType = ``as`` targetType expression

    // await expr
    let ``await`` =
        SyntaxFactory.AwaitExpression

    // default
    let ``default`` targetType = 
        SyntaxFactory.DefaultExpression(ident targetType)

    let ``() =>`` parameters node = 
        let parameterArr = 
            parameters 
            |> Seq.map SyntaxFactory.Identifier
            |> Seq.map SyntaxFactory.Parameter 
            |> SyntaxFactory.SeparatedList
            |> SyntaxFactory.ParameterList
        SyntaxFactory.ParenthesizedLambdaExpression (parameterArr, node)

    let ``() => {}`` parameters block = 
        block
        |> Seq.toArray
        |> SyntaxFactory.Block
        |> ``() =>`` parameters
        
    let ``_ =>`` parameterName expression = 
        SyntaxFactory.SimpleLambdaExpression (parameterName |> (SyntaxFactory.Identifier >> SyntaxFactory.Parameter), expression)

    // make a statement from an expression
    let statement s = 
        SyntaxFactory.ExpressionStatement s
        :> Syntax.StatementSyntax

    // left ?? right
    let (<??>) left right =
        SyntaxFactory.BinaryExpression (SyntaxKind.CoalesceExpression, left, right)
        :> ExpressionSyntax
        
    // left.right
    let (<|.|>) left right = 
        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, right)
        :> ExpressionSyntax

    // left.right(args)
    let (<.>) left (right, args) = 
        ``invoke`` (left <|.|> right) ``(`` args ``)``

    // left?.right
    let (<|?.|>) left right = 
        let member_binding_expr = SyntaxFactory.MemberBindingExpression right
        SyntaxFactory.ConditionalAccessExpression (left, member_binding_expr)
        :> ExpressionSyntax

    // left?.right(args)
    let (<?.>) left (right, args) =
        let member_binding_expr = 
            SyntaxFactory.MemberBindingExpression right :> ExpressionSyntax

        let target = ``invoke`` member_binding_expr ``(`` args ``)``
        SyntaxFactory.ConditionalAccessExpression (left, target)
        :> ExpressionSyntax
        
    // -(expr)
    let ``-`` expr =
        (SyntaxKind.UnaryMinusExpression, SyntaxFactory.ParenthesizedExpression expr) |> SyntaxFactory.PrefixUnaryExpression
        :> ExpressionSyntax

    // left + right
    let (<+>) left right =
        (SyntaxKind.AddExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left - right
    let (<->) left right =
        (SyntaxKind.SubtractExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left * right
    let (<*>) left right =
        (SyntaxKind.MultiplyExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left / right
    let (</>) left right =
        (SyntaxKind.DivideExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax
        
    // left % right
    let (<%>) left right =
        (SyntaxKind.ModuloExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left ^ right
    let (<^>) left right =
        (SyntaxKind.ExclusiveOrExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left == right
    let (.==.) left right =
        (SyntaxKind.EqualsExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left != right
    let (.!=.) left right =
        (SyntaxKind.NotEqualsExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax
        
    // left <= right
    let (.<=.) left right = 
        (SyntaxKind.LessThanOrEqualExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left < right
    let (.<.) left right = 
        (SyntaxKind.LessThanExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left >= right
    let (.>=.) left right = 
        (SyntaxKind.GreaterThanOrEqualExpression,left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left > right
    let (.>.) left right = 
        (SyntaxKind.GreaterThanExpression ,left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left && right
    let (.&&.) left right =
        (SyntaxKind.LogicalAndExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left &&& right
    let (.&&&.) left right =
        (SyntaxKind.BitwiseAndExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left || right
    let (.||.) left right =
        (SyntaxKind.LogicalOrExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left ||| right
    let (.|||.) left right =
        (SyntaxKind.BitwiseOrExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left ^^^ right
    let (.^^^.) left right =
        (SyntaxKind.ExclusiveOrExpression, left, right) |> SyntaxFactory.BinaryExpression
        :> ExpressionSyntax

    // left >>> right
    let (.>>>.) left right =
        (SyntaxKind.RightShiftExpression, left, right) |> SyntaxFactory.BinaryExpression 
        :> ExpressionSyntax

    // left <<< right
    let (.<<<.) left right =
        (SyntaxKind.LeftShiftExpression, left, right) |> SyntaxFactory.BinaryExpression 
        :> ExpressionSyntax

    // ! (expr)
    let (!) expr =
        (SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression expr) |> SyntaxFactory.PrefixUnaryExpression
        :> ExpressionSyntax

    // bitwise NOT
    let ``~~~`` expr =
        (SyntaxKind.BitwiseNotExpression, SyntaxFactory.ParenthesizedExpression expr) |> SyntaxFactory.PrefixUnaryExpression
        :> ExpressionSyntax

    // expr is target
    let ``is`` targetType expression = 
        SyntaxFactory.BinaryExpression (SyntaxKind.IsExpression, expression, ident targetType)
        :> ExpressionSyntax

    // expr is target var
    let ``is assign`` targetType (targetAssign : IdentifierNameSyntax) expression =
        let assign = SyntaxFactory.SingleVariableDesignation targetAssign.Identifier
        SyntaxFactory.IsPatternExpression (expression, SyntaxFactory.DeclarationPattern ((``ident`` targetType), assign))
        :> ExpressionSyntax

    // ( expr )
    let ``))`` = None
    let ``((`` expr ``))`` = 
        SyntaxFactory.ParenthesizedExpression expr
        :> ExpressionSyntax
        
    // single line comment
    let ``//`` comment node =
        [("//" + comment |> SyntaxFactory.Comment); (SyntaxFactory.EndOfLine "")] @ (List.ofSeq ((node :> SyntaxNode).GetLeadingTrivia()))
        |> node.WithLeadingTrivia

    // #line trivia
    let ``#lineNr`` (lineNumber : int) (file : string) =
        SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxFactory.Literal(lineNumber), SyntaxFactory.Literal(file), true))
        
    // #line
    let ``#line`` (lineNumber : int) (file : string) node =
        ``#lineNr`` lineNumber file :: (List.ofSeq ((node :> SyntaxNode).GetLeadingTrivia()))
        |> node.WithLeadingTrivia
        
    // #line hidden
    let ``#line hidden`` node =
        SyntaxFactory.Trivia(SyntaxFactory.LineDirectiveTrivia(SyntaxKind.HiddenKeyword |> SyntaxFactory.Token, true))
        :: (List.ofSeq ((node :> SyntaxNode).GetLeadingTrivia()))
        |> node.WithLeadingTrivia

    let private setArrayArguments (methodArguments : ArgumentSyntax seq) (ie : ElementAccessExpressionSyntax) =
        methodArguments
        |> (SyntaxFactory.SeparatedList >> SyntaxFactory.BracketedArgumentList)
        |> ie.WithArgumentList

    // (val0, val1, ...)
    let ``tuple`` vals = 
        vals 
        |> List.map (fun x -> x :> ExpressionSyntax)
        |> List.map (SyntaxFactory.Argument) 
        |> SyntaxFactory.SeparatedList
        |> SyntaxFactory.TupleExpression :> ExpressionSyntax
        
        
    let ``declare`` (typename:string, id:string) = 
        (typename |> ``type``, id |> SyntaxFactory.Identifier |> SyntaxFactory.SingleVariableDesignation)
        |> SyntaxFactory.DeclarationExpression
        :> ExpressionSyntax 

    // (type1 name1, type2 name2, ...)
    let ``deconstruct`` content = 
        content
        |> List.map (SyntaxFactory.Argument) 
        |> SyntaxFactory.SeparatedList
        |> SyntaxFactory.TupleExpression    
        :> ExpressionSyntax 
        
    // name[args]
    let ``item`` name args =     
        name
        |> SyntaxFactory.ElementAccessExpression
        |> setArrayArguments (args |> Seq.map SyntaxFactory.Argument)
        :> ExpressionSyntax

[<AutoOpen>]
module Statements = 
    open Microsoft.CodeAnalysis
    open Microsoft.CodeAnalysis.CSharp
    open Microsoft.CodeAnalysis.CSharp.Syntax
    
    type SyntaxFactory = SyntaxFactory

    // throw;
    // throw s;
    let ``throw``  eOpt = 
        eOpt 
        |> Option.fold(fun _ e -> SyntaxFactory.ThrowStatement e)  (SyntaxFactory.ThrowStatement ())
        :> StatementSyntax

    // return;
    // return s;
    let ``return`` eOpt = 
        eOpt 
        |> Option.fold(fun _ e -> SyntaxFactory.ReturnStatement e) (SyntaxFactory.ReturnStatement ())
        :> StatementSyntax
        
    // yield return s;
    let ``yield return`` value = 
        SyntaxFactory.YieldStatement (SyntaxKind.YieldReturnStatement, value)
        :> StatementSyntax

    // break;
    let ``break`` = 
        SyntaxFactory.BreakStatement()
        :> StatementSyntax

    // { blocks }
    let ``}}`` = None
    let ``{{`` blocks ``}}`` = 
        blocks
        |> (Seq.toArray >> SyntaxFactory.Block)
        :> Syntax.StatementSyntax
