module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace

type private Pattern =
    | ReturnInResultConditionedBlock of Range QsNullable
    | SetInResultConditionedBlock of string * Range QsNullable
    | ResultEqualityInCondition of Range QsNullable
    | ResultEqualityNotInCondition of Range QsNullable

type private Level = Level of int

let private toCapability inOperation = function
    | ResultEqualityInCondition _ -> if inOperation then RuntimeCapabilities.QPRGen1 else RuntimeCapabilities.Unknown
    | ResultEqualityNotInCondition _ -> RuntimeCapabilities.Unknown
    | ReturnInResultConditionedBlock _ -> RuntimeCapabilities.Unknown
    | SetInResultConditionedBlock _ -> RuntimeCapabilities.Unknown

let private baseCapability = RuntimeCapabilities.QPRGen0

let private level = function
    | RuntimeCapabilities.QPRGen0 -> Level 0
    | RuntimeCapabilities.QPRGen1 -> Level 1
    | _ -> Level 2

let private toDiagnostic context pattern =
    let error code args (range : QsNullable<_>) =
        if level context.Capabilities >= level (toCapability context.IsInOperation pattern)
        then None
        else QsCompilerDiagnostic.Error (code, args) (range.ValueOr Range.Zero) |> Some
    let unsupported =
        if context.Capabilities = RuntimeCapabilities.QPRGen1
        then ErrorCode.ResultComparisonNotInOperationIf
        else ErrorCode.UnsupportedResultComparison

    match pattern with
    | ReturnInResultConditionedBlock range ->
        if context.Capabilities = RuntimeCapabilities.QPRGen1
        then error ErrorCode.ReturnInResultConditionedBlock [ context.ProcessorArchitecture.Value ] range
        else None
    | SetInResultConditionedBlock (name, range) ->
        if context.Capabilities = RuntimeCapabilities.QPRGen1
        then error ErrorCode.SetInResultConditionedBlock [ name; context.ProcessorArchitecture.Value ] range
        else None
    | ResultEqualityInCondition range ->
        error unsupported [ context.ProcessorArchitecture.Value ] range
    | ResultEqualityNotInCondition range ->
        error unsupported [ context.ProcessorArchitecture.Value ] range

let private addOffset offset =
    let add = QsNullable.Map2 (+) offset
    function
    | ReturnInResultConditionedBlock range -> add range |> ReturnInResultConditionedBlock
    | SetInResultConditionedBlock (name, range) -> SetInResultConditionedBlock (name, add range)
    | ResultEqualityInCondition range -> add range |> ResultEqualityInCondition
    | ResultEqualityNotInCondition range -> add range |> ResultEqualityNotInCondition

let private locationOffset = QsNullable<_>.Map (fun (location : QsLocation) -> location.Offset)

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

let private expressionPatterns inCondition (expression : TypedExpression) =
    expression.ExtractAll <| fun expression' ->
        if isResultEquality expression'
        then
            expression'.Range
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
    |> Seq.collect (fun group -> group |> Seq.map (fun location -> group.Key, location.Offset + location.Range))
    |> Seq.filter (fst >> isKnownSymbol)

let private conditionBlocks condBlocks elseBlock =
    elseBlock
    |> QsNullable<_>.Map (fun block -> SyntaxGenerator.BoolLiteral true, block)
    |> QsNullable<_>.Fold (fun acc x -> x :: acc) []
    |> Seq.append condBlocks

/// Verifies that any conditional blocks which depend on a measurement result do not use any language constructs that
/// are not supported by the runtime capabilities. Returns the diagnostics for the blocks.
let private conditionalStatementPatterns { ConditionalBlocks = condBlocks; Default = elseBlock } =
    let returnStatements (statement : QsStatement) = statement.ExtractAll <| fun s ->
        match s.Statement with
        | QsReturnStatement _ -> [ s ]
        | _ -> []
    let returnPatterns (block : QsPositionedBlock) =
        block.Body.Statements
        |> Seq.collect returnStatements
        |> Seq.map (fun statement ->
               let range = statement.Location |> QsNullable<_>.Map (fun location -> location.Offset + location.Range)
               ReturnInResultConditionedBlock range)
    let setPatterns (block : QsPositionedBlock) =
        nonLocalUpdates block.Body
        |> Seq.map (fun (name, range) -> SetInResultConditionedBlock (name.Value, Value range))
    let foldPatterns (dependsOnResult, diagnostics) (condition : TypedExpression, block : QsPositionedBlock) =
        if dependsOnResult || condition.Exists isResultEquality
        then true, Seq.concat [ diagnostics; returnPatterns block; setPatterns block ]
        else false, diagnostics

    conditionBlocks condBlocks elseBlock
    |> Seq.fold foldPatterns (false, Seq.empty)
    |> snd

let private statementPatterns statement =
    let patterns = ResizeArray ()
    let mutable offset = Null
    let transformation = SyntaxTreeTransformation TransformationOptions.NoRebuild

    transformation.Statements <- {
        new StatementTransformation (transformation, TransformationOptions.NoRebuild) with
            override this.OnLocation location =
                offset <- locationOffset location
                location
    }
    transformation.StatementKinds <- {
        new StatementKindTransformation (transformation, TransformationOptions.NoRebuild) with
            override this.OnConditionalStatement statement =
                conditionalStatementPatterns statement |> patterns.AddRange
                for condition, block in conditionBlocks statement.ConditionalBlocks statement.Default do
                    let blockOffset = locationOffset block.Location
                    expressionPatterns true condition |> Seq.map (addOffset blockOffset) |> patterns.AddRange
                    this.Transformation.Statements.OnScope block.Body |> ignore
                QsConditionalStatement statement
    }
    transformation.Expressions <- {
        new ExpressionTransformation (transformation, TransformationOptions.NoRebuild) with
            override this.OnTypedExpression expression =
                expressionPatterns false expression |> Seq.map (addOffset offset) |> patterns.AddRange
                expression
    }
    transformation.Statements.OnStatement statement |> ignore
    patterns

let private scopePatterns scope = scope.Statements |> Seq.collect statementPatterns

let ScopeDiagnostics context scope = scopePatterns scope |> Seq.choose (toDiagnostic context)

let private maxCapability capabilities =
    if Seq.isEmpty capabilities
    then baseCapability
    else capabilities |> Seq.maxBy level

let private specializationCapability inOperation specialization =
    match specialization.Implementation with
    | Provided (_, scope) ->
        let offset = specialization.Location |> QsNullable<_>.Map (fun location -> location.Offset)
        scopePatterns scope |> Seq.map (addOffset offset >> toCapability inOperation) |> maxCapability
    | _ -> baseCapability

let private callableCapability callable =
    let inOperation =
        match callable.Kind with
        | Operation -> true
        | _ -> false
    callable.Specializations |> Seq.map (specializationCapability inOperation) |> maxCapability

let InferCapabilities compilation =
    let transformation = SyntaxTreeTransformation ()
    transformation.Namespaces <- {
        new NamespaceTransformation (transformation) with
            override this.OnCallableDeclaration callable =
                let capability = callableCapability callable
                let arg =
                    SyntaxGenerator.StringLiteral (capability.ToString () |> NonNullable<_>.New, ImmutableArray.Empty)
                let attribute = AttributeUtils.BuildAttribute (BuiltIn.Capability.FullName, arg)
                { callable with Attributes = callable.Attributes.Add attribute }
    }
    transformation.OnCompilation compilation
