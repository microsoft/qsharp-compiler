// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens

type SymbolOccurrence =
    private
    | Declaration of QsSymbol
    | UsedType of QsType
    | UsedVariable of QsSymbol
    | UsedLiteral of QsExpression

module SymbolOccurrence =
    let rec private flattenSymbol symbol =
        match symbol.Symbol with
        | SymbolTuple items -> Seq.collect flattenSymbol items |> Seq.toList
        | _ -> [ symbol ]

    let private inExpression expression = failwith ""

    let private inType type_ = failwith ""

    let private inInitializer initializer = failwith ""

    let private inGenerator generator = failwith ""

    let private inNamedTuple tuple = failwith ""

    let private inCallable (callable: CallableDeclaration) =
        let typeParams = Seq.map Declaration callable.Signature.TypeParameters |> Seq.toList
        let args = inNamedTuple callable.Signature.Argument
        let returnType = inType callable.Signature.ReturnType
        Declaration callable.Name :: typeParams @ args @ returnType

    [<CompiledName "InFragment">]
    let inFragment fragment =
        match fragment with
        | NamespaceDeclaration s -> [ Declaration s ]
        | OpenDirective (_, s) -> QsNullable<_>.Choose id [ s ] |> Seq.toList |> List.map Declaration
        | DeclarationAttribute (s, e) -> UsedVariable s :: inExpression e
        | OperationDeclaration c
        | FunctionDeclaration c -> inCallable c
        | BodyDeclaration g
        | AdjointDeclaration g
        | ControlledDeclaration g
        | ControlledAdjointDeclaration g -> inGenerator g
        | ImmutableBinding (s, e)
        | MutableBinding (s, e)
        | ForLoopIntro (s, e) -> List.map Declaration (flattenSymbol s) @ inExpression e
        | UsingBlockIntro (s, i)
        | BorrowingBlockIntro (s, i) -> List.map Declaration (flattenSymbol s) @ inInitializer i
        | TypeDefinition t -> Declaration t.Name :: inNamedTuple t.UnderlyingType
        | ExpressionStatement e
        | ReturnStatement e
        | FailStatement e
        | IfClause e
        | ElifClause e
        | WhileLoopIntro e
        | UntilSuccess (e, _) -> inExpression e
        | ValueUpdate (e1, e2) -> inExpression e1 @ inExpression e2
        | ElseClause
        | RepeatIntro
        | WithinBlockIntro
        | ApplyBlockIntro
        | InvalidFragment -> []
