namespace QsFmt.Formatter.ParseTree

open QsFmt.Formatter.SyntaxTree
open QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="SymbolBinding"/> nodes from a parse tree and the list of tokens.
/// </summary>
type private SymbolBindingVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SymbolBinding>()

    override _.DefaultResult = failwith "Unknown symbol binding."

    override _.VisitSymbolName context =
        { Name = context.name |> Node.toTerminal tokens; Type = None } |> SymbolDeclaration

    override visitor.VisitSymbolTuple context =
        let bindings = context._bindings |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems bindings commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SymbolTuple

/// <summary>
/// Creates syntax tree <see cref="Statement"/> nodes from a parse tree and the list of tokens.
/// </summary>
type internal StatementVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Statement>()

    let expressionVisitor = ExpressionVisitor tokens

    let symbolBindingVisitor = SymbolBindingVisitor tokens

    override _.DefaultResult = failwith "Unknown statement."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Statement.Unknown

    override _.VisitReturnStatement context =
        {
            ReturnKeyword = context.``return`` |> Node.toTerminal tokens
            Expression = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Return

    override _.VisitLetStatement context =
        {
            LetKeyword = context.``let`` |> Node.toTerminal tokens
            Binding = symbolBindingVisitor.Visit context.binding
            Equals = context.equals |> Node.toTerminal tokens
            Value = expressionVisitor.Visit context.value
            Semicolon = context.semicolon |> Node.toTerminal tokens
        }
        |> Let

    override visitor.VisitIfStatement context =
        {
            IfKeyword = context.``if`` |> Node.toTerminal tokens
            Condition = expressionVisitor.Visit context.condition
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> If

    override visitor.VisitElseStatement context =
        {
            ElseKeyword = context.``else`` |> Node.toTerminal tokens
            Block =
                {
                    OpenBrace = context.body.openBrace |> Node.toTerminal tokens
                    Items = context.body._statements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.body.closeBrace |> Node.toTerminal tokens
                }
        }
        |> Else
