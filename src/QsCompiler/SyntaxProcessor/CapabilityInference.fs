module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace

type Inference =
    | ReturnInResultConditionedBlock of Range QsNullable
    | SetInResultConditionedBlock of string * Range QsNullable
    | ResultEqualityInCondition of Range QsNullable
    | ResultEqualityNotInCondition of Range QsNullable

let private capability = function
    | ResultEqualityInCondition _ -> RuntimeCapabilities.QPRGen1
    | ResultEqualityNotInCondition _ -> RuntimeCapabilities.Unknown
    | ReturnInResultConditionedBlock _ -> RuntimeCapabilities.Unknown
    | SetInResultConditionedBlock _ -> RuntimeCapabilities.Unknown

/// Returns true if the expression is an equality or inequality comparison between two expressions of type Result.
let private isResultEquality { TypedExpression.Expression = expression } =
    let validType = function
        | InvalidType -> None
        | kind -> Some kind
    let binaryType lhs rhs =
        validType lhs.ResolvedType.Resolution
        |> Option.defaultValue rhs.ResolvedType.Resolution

    // This assumes that:
    // - Result has no derived types that support equality comparisons.
    // - Compound types containing Result (e.g., tuples or arrays of results) do not support equality comparison.
    match expression with
    | EQ (lhs, rhs)
    | NEQ (lhs, rhs) -> binaryType lhs rhs = Result
    | _ -> false

let private expressionInferences inCondition offset (expression : TypedExpression) =
    expression.ExtractAll <| fun expression' ->
        if isResultEquality expression'
        then
            expression'.Range
            |> QsNullable.Map2 (+) offset
            |> if inCondition then ResultEqualityInCondition else ResultEqualityNotInCondition
            |> Seq.singleton
        else Seq.empty

/// Finds the locations where a mutable variable, which was not declared locally in the given scope, is reassigned.
/// Returns the name of the variable and the location of the reassignment.
let private nonLocalUpdates scope =
    let isKnownSymbol name =
        scope.KnownSymbols.Variables
        |> Seq.exists (fun variable -> variable.VariableName = name)

    let accumulator = AccumulateIdentifiers ()
    accumulator.Statements.OnScope scope |> ignore
    accumulator.SharedState.ReassignedVariables
    |> Seq.collect (fun grouping -> grouping |> Seq.map (fun location -> grouping.Key, location))
    |> Seq.filter (fst >> isKnownSymbol)

let private conditionBlocks condBlocks elseBlock =
    elseBlock
    |> QsNullable<_>.Map (fun block -> SyntaxGenerator.BoolLiteral true, block)
    |> QsNullable<_>.Fold (fun acc x -> x :: acc) []
    |> Seq.append condBlocks

/// Verifies that any conditional blocks which depend on a measurement result do not use any language constructs that
/// are not supported by the runtime capabilities. Returns the diagnostics for the blocks.
let private conditionalStatementInferences { ConditionalBlocks = condBlocks; Default = elseBlock } =
    let returnStatements (statement : QsStatement) = statement.ExtractAll <| fun s ->
        match s.Statement with
        | QsReturnStatement _ -> [ s ]
        | _ -> []
    let returnInferences (block : QsPositionedBlock) =
        block.Body.Statements
        |> Seq.collect returnStatements
        |> Seq.map (fun statement ->
               let range = statement.Location |> QsNullable<_>.Map (fun location -> location.Offset + location.Range)
               ReturnInResultConditionedBlock range)
    let setInferences (block : QsPositionedBlock) =
        nonLocalUpdates block.Body
        |> Seq.map (fun (name, location) ->
               SetInResultConditionedBlock (name.Value, location.Offset + location.Range |> Value))
    let foldInferences (dependsOnResult, diagnostics) (condition : TypedExpression, block : QsPositionedBlock) =
        let offset = block.Location |> QsNullable<_>.Map (fun location -> location.Offset)
        let conditionInferences = expressionInferences true offset condition
        if dependsOnResult || condition.Exists isResultEquality
        then true, Seq.concat [ diagnostics; conditionInferences; returnInferences block; setInferences block ]
        else false, Seq.append diagnostics conditionInferences

    conditionBlocks condBlocks elseBlock
    |> Seq.fold foldInferences (false, Seq.empty)
    |> snd

let Inferences compilation =
    let inferences = ResizeArray ()
    let mutable offset = Null
    let transformation = SyntaxTreeTransformation TransformationOptions.NoRebuild

    transformation.Statements <- {
        new StatementTransformation (transformation, TransformationOptions.NoRebuild) with
            member this.OnLocation location =
                offset <- location |> QsNullable<_>.Map (fun location' -> location'.Offset)
                location
    }
    transformation.StatementKinds <- {
        new StatementKindTransformation (transformation, TransformationOptions.NoRebuild) with
            member this.OnConditionalStatement statement =
                conditionalStatementInferences statement |> inferences.AddRange
                for _, block in conditionBlocks statement.ConditionalBlocks statement.Default do
                    this.Transformation.Statements.OnScope block.Body |> ignore
                QsConditionalStatement statement
    }
    transformation.Expressions <- {
        new ExpressionTransformation (transformation, TransformationOptions.NoRebuild) with
            member this.OnTypedExpression expression =
                expressionInferences false offset expression |> inferences.AddRange
                expression
    }

    transformation.OnCompilation compilation |> ignore
    inferences
