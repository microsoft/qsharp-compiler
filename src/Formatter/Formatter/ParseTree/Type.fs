namespace QsFmt.Formatter.ParseTree

open QsFmt.Formatter.SyntaxTree
open QsFmt.Parser

/// <summary>
/// Creates syntax tree <see cref="Type"/> nodes from a parse tree and the list of tokens.
/// </summary>
type internal TypeVisitor(tokens) =
    inherit QSharpParserBaseVisitor<Type>()

    override _.DefaultResult = failwith "Unknown type."

    override _.VisitChildren node =
        Node.toUnknown tokens node |> Type.Unknown

    override _.VisitIntType context =
        context.Int().Symbol
        |> Node.toTerminal tokens
        |> BuiltIn

    override _.VisitUserDefinedType context =
        { Prefix = Node.prefix tokens context.name.Start.TokenIndex
          Text = context.name.GetText() }
        |> UserDefined
