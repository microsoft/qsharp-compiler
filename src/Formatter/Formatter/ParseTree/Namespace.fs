namespace QsFmt.Formatter.ParseTree

open QsFmt.Formatter.SyntaxTree
open QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="SymbolBinding"/> nodes for callable parameters from a parse tree and the list of
/// tokens.
/// </summary>
type private ParameterVisitor(tokens) =
    inherit QSharpParserBaseVisitor<SymbolBinding>()

    let typeVisitor = TypeVisitor tokens

    override _.DefaultResult = failwith "Unknown symbol binding."

    override visitor.VisitNamedParameter context =
        context.namedItem () |> visitor.VisitNamedItem

    override _.VisitNamedItem context =
        {
            Name = context.name |> Node.toTerminal tokens
            Type =
                { Colon = context.colon |> Node.toTerminal tokens; Type = context.itemType |> typeVisitor.Visit }
                |> Some
        }
        |> SymbolDeclaration

    override visitor.VisitParameterTuple context =
        let parameters = context._parameters |> Seq.map visitor.Visit
        let commas = context._commas |> Seq.map (Node.toTerminal tokens)

        {
            OpenParen = context.openParen |> Node.toTerminal tokens
            Items = Node.tupleItems parameters commas
            CloseParen = context.closeParen |> Node.toTerminal tokens
        }
        |> SymbolTuple

/// <summary>
/// Creates syntax tree <see cref="NamespaceItem"/> nodes from a parse tree and the list of tokens.
/// </summary>
type private NamespaceItemVisitor(tokens) =
    inherit QSharpParserBaseVisitor<NamespaceItem>()

    let parameterVisitor = ParameterVisitor tokens
    let typeVisitor = TypeVisitor tokens
    let statementVisitor = StatementVisitor tokens

    override _.DefaultResult = failwith "Unknown namespace element."

    override _.VisitChildren node = Node.toUnknown tokens node |> Unknown

    override _.VisitCallableElement context =
        let scope = context.callable.body.scope () // TODO

        {
            CallableKeyword = context.callable.keyword |> Node.toTerminal tokens
            Name = context.callable.name |> Node.toTerminal tokens
            Parameters = parameterVisitor.Visit context.callable.tuple
            ReturnType =
                {
                    Colon = context.callable.colon |> Node.toTerminal tokens
                    Type = typeVisitor.Visit context.callable.returnType
                }
            Block =
                {
                    OpenBrace = scope.openBrace |> Node.toTerminal tokens
                    Items = scope._statements |> Seq.map statementVisitor.Visit |> List.ofSeq
                    CloseBrace = scope.closeBrace |> Node.toTerminal tokens
                }
        }
        |> CallableDeclaration

/// <summary>
/// Constructors for syntax tree <see cref="Namespace"/> and <see cref="Document"/> nodes.
/// </summary>
module internal Namespace =
    /// <summary>
    /// Creates a syntax tree <see cref="Namespace"/> node from the parse tree
    /// <see cref="QSharpParser.NamespaceContext"/> node and the list of tokens.
    /// </summary>
    let private toNamespace tokens (context: QSharpParser.NamespaceContext) =
        let visitor = NamespaceItemVisitor tokens

        {
            NamespaceKeyword = context.keyword |> Node.toTerminal tokens
            Name = { Prefix = Node.prefix tokens context.name.Start.TokenIndex; Text = context.name.GetText() }
            Block =
                {
                    OpenBrace = context.openBrace |> Node.toTerminal tokens
                    Items = context._elements |> Seq.map visitor.Visit |> List.ofSeq
                    CloseBrace = context.closeBrace |> Node.toTerminal tokens
                }
        }

    /// <summary>
    /// Creates a syntax tree <see cref="Document"/> node from the parse tree <see cref="QSharpParser.DocumentContext"/>
    /// node and the list of tokens.
    /// </summary>
    let toDocument tokens (context: QSharpParser.DocumentContext) =
        let namespaces = context.``namespace`` () |> Array.toList |> List.map (toNamespace tokens)
        let eof = { (context.eof |> Node.toTerminal tokens) with Text = "" }
        { Namespaces = namespaces; Eof = eof }
