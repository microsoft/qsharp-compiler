module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.CallGraphWalker
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

/// A syntactic pattern that requires a runtime capability.
type private Pattern =
    /// A return statement in the block of an if statement whose condition depends on a result.
    | ReturnInResultConditionedBlock of Range QsNullable

    /// A set statement in the block of an if statement whose condition depends on a result, but which is reassigning a
    /// variable declared outside the block. Includes the name of the variable.
    | SetInResultConditionedBlock of string * Range QsNullable

    /// An equality expression inside the condition of an if statement, where the operands are results
    | ResultEqualityInCondition of Range QsNullable

    /// An equality expression outside the condition of an if statement, where the operands are results.
    | ResultEqualityNotInCondition of Range QsNullable

/// The level of a runtime capability. Higher levels require more capabilities.
type private Level = Level of int

/// Returns the required runtime capability of the pattern, given whether it occurs in an operation.
let private toCapability inOperation = function
    | ResultEqualityInCondition _ -> if inOperation then RuntimeCapabilities.QPRGen1 else RuntimeCapabilities.Unknown
    | ResultEqualityNotInCondition _ -> RuntimeCapabilities.Unknown
    | ReturnInResultConditionedBlock _ -> RuntimeCapabilities.Unknown
    | SetInResultConditionedBlock _ -> RuntimeCapabilities.Unknown

/// The runtime capability with the lowest level.
let private baseCapability = RuntimeCapabilities.QPRGen0

/// Returns the level of the runtime capability.
let private level = function
    | RuntimeCapabilities.QPRGen0 -> Level 0
    | RuntimeCapabilities.QPRGen1 -> Level 1
    | _ -> Level 2

/// Returns a diagnostic for the pattern if the inferred capability level exceeds the execution target's capability
/// level.
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

/// Adds a position offset to the range in the pattern.
let private addOffset offset =
    let add = QsNullable.Map2 (+) offset
    function
    | ReturnInResultConditionedBlock range -> add range |> ReturnInResultConditionedBlock
    | SetInResultConditionedBlock (name, range) -> SetInResultConditionedBlock (name, add range)
    | ResultEqualityInCondition range -> add range |> ResultEqualityInCondition
    | ResultEqualityNotInCondition range -> add range |> ResultEqualityNotInCondition

/// Returns the offset of a nullable location.
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

/// Returns all patterns in the expression, given whether it occurs in an if condition. Ranges are relative to the start
/// of the statement.
let private expressionPatterns inCondition (expression : TypedExpression) =
    expression.ExtractAll <| fun expression' ->
        if isResultEquality expression'
        then
            expression'.Range
            |> if inCondition then ResultEqualityInCondition else ResultEqualityNotInCondition
            |> Seq.singleton
        else Seq.empty

/// Finds the locations where a mutable variable, which was not declared locally in the given scope, is reassigned.
/// Returns the name of the variable and the range of the reassignment.
let private nonLocalUpdates scope =
    let isKnownSymbol name =
        scope.KnownSymbols.Variables
        |> Seq.exists (fun variable -> variable.VariableName = name)

    let accumulator = AccumulateIdentifiers ()
    accumulator.Statements.OnScope scope |> ignore
    accumulator.SharedState.ReassignedVariables
    |> Seq.collect (fun group -> group |> Seq.map (fun location -> group.Key, location.Offset + location.Range))
    |> Seq.filter (fst >> isKnownSymbol)

/// Converts the conditional blocks and an optional else block into a single sequence, where the else block is
/// equivalent to "elif (true) { ... }".
let private conditionBlocks condBlocks elseBlock =
    elseBlock
    |> QsNullable<_>.Map (fun block -> SyntaxGenerator.BoolLiteral true, block)
    |> QsNullable<_>.Fold (fun acc x -> x :: acc) []
    |> Seq.append condBlocks

/// Returns all patterns in the conditional statement that are relevant to its conditions. It does not return patterns
/// for the conditions themselves, or the patterns of nested conditional statements.
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

/// Returns all patterns in the statement. Ranges are relative to the start of the specialization.
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

/// Returns all patterns in the scope. Ranges are relative to the start of the specialization.
let private scopePatterns scope = scope.Statements |> Seq.collect statementPatterns

/// Returns all capability diagnostics for the scope. Ranges are relative to the start of the specialization.
let ScopeDiagnostics context scope = scopePatterns scope |> Seq.choose (toDiagnostic context)

/// Returns the maximum capability in the sequence of capabilities, or none if the sequence is empty.
let private tryMaxCapability capabilities =
    if Seq.isEmpty capabilities
    then None
    else capabilities |> Seq.maxBy level |> Some

/// Returns the maximum capability in the sequence of capabilities, or the base capability if the sequence is empty.
let private maxCapability = tryMaxCapability >> Option.defaultValue baseCapability

/// Looks up a key in the dictionary, returning Some value if it is found and None if not.
let private tryGetValue key (dict : IReadOnlyDictionary<_, _>) =
    match dict.TryGetValue key with
    | true, value -> Some value
    | false, _ -> None

/// Returns true if the callable is an operation.
let private isOperation callable =
    match callable.Kind with
    | Operation -> true
    | _ -> false

/// Returns true if the callable is declared in a source file in the current compilation, instead of a referenced
/// library.
let private isDeclaredInSourceFile (callable : QsCallable) =
    callable.SourceFile.Value.EndsWith ".qs"

/// Given whether the specialization is part of an operation, returns its required capability based on its source code,
/// ignoring callable dependencies.
let private specSourceCapability inOperation spec =
    match spec.Implementation with
    | Provided (_, scope) ->
        let offset = spec.Location |> QsNullable<_>.Map (fun location -> location.Offset)
        scopePatterns scope |> Seq.map (addOffset offset >> toCapability inOperation) |> maxCapability
    | _ -> baseCapability

/// Returns the required runtime capability of the callable based on its source code, ignoring callable dependencies.
let private callableSourceCapability callable =
    callable.Specializations
    |> Seq.map (isOperation callable |> specSourceCapability)
    |> maxCapability

/// Returns the required capability of the callable based on its capability attribute if one is present. If no attribute
/// is present and the callable is not defined in a reference, returns the capability based on its source code and
/// callable dependencies. Otherwise, returns the base capability.
///
/// Partially applying the first argument creates a memoized function that caches computed runtime capabilities by
/// callable name. The memoized function is not thread-safe.
let private callableDependentCapability (callables : IImmutableDictionary<_, QsCallable>, graph : CallGraph) =
    // A mapping from callable name to runtime capability based on callable source code patterns and cycles the callable
    // is a member of, but not other dependencies. This is the initial set of capabilities that will be used later.
    let initialCapabilities =
        callables
        |> Seq.filter (fun item -> isDeclaredInSourceFile item.Value)
        |> fun items -> items.ToDictionary ((fun item -> item.Key), fun item -> callableSourceCapability item.Value)
    let sourceCycles =
        graph.GetCallCycles ()
        |> Seq.filter (Seq.exists <| fun node ->
            callables |> tryGetValue node.CallableName |> Option.exists isDeclaredInSourceFile)
    for cycle in sourceCycles do
        let cycleCapability =
            cycle
            |> Seq.choose (fun node -> callables |> tryGetValue node.CallableName)
            |> Seq.map callableSourceCapability
            |> maxCapability
        for node in cycle do
            initialCapabilities.[node.CallableName] <-
                maxCapability [ initialCapabilities.[node.CallableName]; cycleCapability ]

    // The memoization cache.
    let cache = Dictionary<_, _> ()

    // The capability of a specialization based on its initial capability and the capability of all dependencies.
    let rec specCapability visited (spec : QsSpecialization) =
        let visited = Set.add spec.Parent visited
        let dependencies =
            graph.GetDirectDependencies spec
            |> Seq.map (fun group -> group.Key.CallableName)
            |> Set.ofSeq
            |> fun names -> Set.difference names visited
        dependencies
        |> Seq.choose (fun name -> callables |> tryGetValue name)
        |> Seq.map (callableCapability visited)
        |> Seq.append (initialCapabilities |> tryGetValue spec.Parent |> Option.toList)
        |> maxCapability

    // The capability of a callable based on its initial capability and the capability of all dependencies.
    and callableCapability visited (callable : QsCallable) =
        cache
        |> tryGetValue callable.FullName
        |> Option.defaultWith (fun () ->
            let capability =
                callable.Attributes
                |> QsNullable<_>.Choose BuiltIn.GetCapability
                |> tryMaxCapability
                |> Option.defaultWith (fun () ->
                    if isDeclaredInSourceFile callable
                    then callable.Specializations |> Seq.map (specCapability visited) |> maxCapability
                    else baseCapability)
            cache.[callable.FullName] <- capability
            capability)

    callableCapability Set.empty

/// Adds the given capability to the callable as an attribute.
let private addAttribute callable capability =
    let arg = capability.ToString () |> AttributeUtils.StringArgument
    let attribute = AttributeUtils.BuildAttribute (BuiltIn.Capability.FullName, arg)
    { callable with QsCallable.Attributes = callable.Attributes.Add attribute }

/// Infers the capability of all callables in the compilation, adding the built-in Capability attribute to each
/// callable.
let InferCapabilities compilation =
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = BuildCallGraph.Apply compilation
    let transformation = SyntaxTreeTransformation ()
    let callableCapability = callableDependentCapability (callables, graph)
    transformation.Namespaces <- {
        new NamespaceTransformation (transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability =
                    callable.Attributes |> QsNullable<_>.Choose BuiltIn.GetCapability |> Seq.isEmpty
                if isMissingCapability && isDeclaredInSourceFile callable
                then callableCapability callable |> addAttribute callable
                else callable
    }
    transformation.OnCompilation compilation
