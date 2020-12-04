// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
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

/// Returns the offset of a nullable location.
let private locationOffset = QsNullable<_>.Map (fun (location : QsLocation) -> location.Offset)

/// Tracks the most recently seen statement location.
type private StatementLocationTracker (parent, options) =
    inherit StatementTransformation (parent, options)

    let mutable offset = Null

    /// The offset of the most recently seen statement location.
    member this.Offset = offset

    override this.OnLocation location =
        offset <- locationOffset location
        base.OnLocation location

/// Returns the required runtime capability of the pattern, given whether it occurs in an operation.
let private patternCapability inOperation = function
    | ResultEqualityInCondition _ -> if inOperation then BasicMeasurementFeedback else FullComputation
    | ResultEqualityNotInCondition _ -> FullComputation
    | ReturnInResultConditionedBlock _ -> FullComputation
    | SetInResultConditionedBlock _ -> FullComputation

/// Returns the joined capability of the sequence of capabilities, or the default capability if the sequence is empty.
let private joinCapabilities = Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

/// Returns a diagnostic for the pattern if the inferred capability level exceeds the execution target's capability
/// level.
let private patternDiagnostic context pattern =
    let error code args (range : _ QsNullable) =
        if patternCapability context.IsInOperation pattern |> context.Capability.Implies
        then None
        else QsCompilerDiagnostic.Error (code, args) (range.ValueOr Range.Zero) |> Some
    let unsupported =
        if context.Capability = BasicMeasurementFeedback
        then ErrorCode.ResultComparisonNotInOperationIf
        else ErrorCode.UnsupportedResultComparison

    match pattern with
    | ReturnInResultConditionedBlock range ->
        if context.Capability = BasicMeasurementFeedback
        then error ErrorCode.ReturnInResultConditionedBlock [ context.ProcessorArchitecture ] range
        else None
    | SetInResultConditionedBlock (name, range) ->
        if context.Capability = BasicMeasurementFeedback
        then error ErrorCode.SetInResultConditionedBlock [ name; context.ProcessorArchitecture ] range
        else None
    | ResultEqualityInCondition range ->
        error unsupported [ context.ProcessorArchitecture ] range
    | ResultEqualityNotInCondition range ->
        error unsupported [ context.ProcessorArchitecture ] range

/// Adds a position offset to the range in the pattern.
let private addOffset offset =
    let add = QsNullable.Map2 (+) offset
    function
    | ReturnInResultConditionedBlock range -> add range |> ReturnInResultConditionedBlock
    | SetInResultConditionedBlock (name, range) -> SetInResultConditionedBlock (name, add range)
    | ResultEqualityInCondition range -> add range |> ResultEqualityInCondition
    | ResultEqualityNotInCondition range -> add range |> ResultEqualityNotInCondition

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
        |> Seq.map (fun (name, range) -> SetInResultConditionedBlock (name, Value range))
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
    let transformation = SyntaxTreeTransformation TransformationOptions.NoRebuild
    let location = StatementLocationTracker (transformation, TransformationOptions.NoRebuild)
    transformation.Statements <- location
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
                expressionPatterns false expression |> Seq.map (addOffset location.Offset) |> patterns.AddRange
                expression
    }
    transformation.Statements.OnStatement statement |> ignore
    patterns

/// Returns all patterns in the scope. Ranges are relative to the start of the specialization.
let private scopePatterns scope = scope.Statements |> Seq.collect statementPatterns

/// Returns a list of the names of global callables referenced in the scope, and the range of the reference relative to
/// the start of the specialization.
let private globalReferences scope =
    let mutable references = []
    let transformation = SyntaxTreeTransformation TransformationOptions.NoRebuild
    let location = StatementLocationTracker (transformation, TransformationOptions.NoRebuild)
    transformation.Statements <- location
    transformation.Expressions <- {
        new ExpressionTransformation (transformation, TransformationOptions.NoRebuild) with
            override this.OnTypedExpression expression =
                match expression.Expression with
                | Identifier (GlobalCallable name, _) ->
                    let range = QsNullable.Map2 (+) location.Offset expression.Range
                    references <- (name, range) :: references
                | _ -> ()
                base.OnTypedExpression expression
    }
    transformation.Statements.OnScope scope |> ignore
    references

/// Returns a diagnostic for a reference to a global callable with the given name based on its capability attribute and
/// the context's supported runtime capabilities.
let private referenceDiagnostic context (name, range : _ QsNullable) =
    match context.Globals.TryGetCallable name (context.Symbols.Parent.Namespace, context.Symbols.SourceFile) with
    | Found declaration ->
        let capability = (SymbolResolution.TryGetRequiredCapability declaration.Attributes).ValueOr RuntimeCapability.Base
        if context.Capability.Implies capability
        then None
        else
            let error = ErrorCode.UnsupportedCapability, [ name.Name; string capability; context.ProcessorArchitecture ]
            range.ValueOr Range.Zero |> QsCompilerDiagnostic.Error error |> Some
    | _ -> None

/// Returns all capability diagnostics for the scope. Ranges are relative to the start of the specialization.
let ScopeDiagnostics context scope =
    [ globalReferences scope |> Seq.choose (referenceDiagnostic context)
      scopePatterns scope |> Seq.choose (patternDiagnostic context) ]
    |> Seq.concat

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

/// Returns true if the QsNullable is null.
let private isQsNull = function
    | Value _ -> false
    | Null -> true

/// Returns true if the callable is declared in a source file in the current compilation, instead of a referenced
/// library.
let private isDeclaredInSourceFile (callable : QsCallable) = callable.SourceFile.EndsWith ".qs"

/// Given whether the specialization is part of an operation, returns its required capability based on its source code,
/// ignoring callable dependencies.
let private specSourceCapability inOperation spec =
    match spec.Implementation with
    | Provided (_, scope) ->
        let offset = spec.Location |> QsNullable<_>.Map (fun location -> location.Offset)
        scopePatterns scope |> Seq.map (addOffset offset >> patternCapability inOperation) |> joinCapabilities
    | _ -> RuntimeCapability.Base

/// Returns the required runtime capability of the callable based on its source code, ignoring callable dependencies.
let private callableSourceCapability callable =
    callable.Specializations
    |> Seq.map (isOperation callable |> specSourceCapability)
    |> joinCapabilities

/// Returns the required capability of the callable based on its capability attribute if one is present. If no attribute
/// is present and the callable is not defined in a reference, returns the capability based on its source code and
/// callable dependencies. Otherwise, returns the base capability.
///
/// Partially applying the first argument creates a memoized function that caches computed runtime capabilities by
/// callable name. The memoized function is not thread-safe.
let private callableDependentCapability (callables : IImmutableDictionary<_, _>, graph : CallGraph) =
    // A mapping from callable name to runtime capability based on callable source code patterns and cycles the callable
    // is a member of, but not other dependencies. This is the initial set of capabilities that will be used later.
    let initialCapabilities =
        callables
        |> Seq.filter (fun item -> isDeclaredInSourceFile item.Value)
        |> fun items -> items.ToDictionary ((fun item -> item.Key), fun item -> callableSourceCapability item.Value)
    let sourceCycles =
        graph.GetCallCycles () |> Seq.filter (Seq.exists <| fun node ->
            callables |> tryGetValue node.CallableName |> Option.exists isDeclaredInSourceFile)
    for cycle in sourceCycles do
        let cycleCapability =
            cycle
            |> Seq.choose (fun node -> callables |> tryGetValue node.CallableName)
            |> Seq.map callableSourceCapability
            |> joinCapabilities
        for node in cycle do
            initialCapabilities.[node.CallableName] <-
                joinCapabilities [ initialCapabilities.[node.CallableName]; cycleCapability ]

    // The memoization cache.
    let cache = Dictionary<_, _> ()

    // The capability of a callable's dependencies.
    let rec dependentCapability visited name =
        let visited = Set.add name visited
        let newDependencies =
            CallGraphNode name
            |> graph.GetDirectDependencies
            |> Seq.map (fun group -> group.Key.CallableName)
            |> Set.ofSeq
            |> fun names -> Set.difference names visited
        newDependencies
        |> Seq.choose (fun name -> callables |> tryGetValue name)
        |> Seq.map (cachedCapability visited)
        |> joinCapabilities

    // The capability of a callable based on its initial capability and the capability of all dependencies.
    and callableCapability visited (callable : QsCallable) =
        (SymbolResolution.TryGetRequiredCapability callable.Attributes).ValueOrApply (fun () ->
            if isDeclaredInSourceFile callable
            then
                [ initialCapabilities |> tryGetValue callable.FullName |> Option.defaultValue RuntimeCapability.Base
                  dependentCapability visited callable.FullName ]
                |> joinCapabilities
            else RuntimeCapability.Base)

    // Tries to retrieve the capability of the callable from the cache first; otherwise, computes the capability and
    // saves it in the cache.
    and cachedCapability visited (callable : QsCallable) =
        cache |> tryGetValue callable.FullName |> Option.defaultWith (fun () ->
            let capability = callableCapability visited callable
            cache.[callable.FullName] <- capability
            capability)

    cachedCapability Set.empty

/// Returns the attribute for the inferred runtime capability.
let private toAttribute capability =
    let args = AttributeUtils.StringArguments (capability.ToString (), "Inferred automatically by the compiler.")
    AttributeUtils.BuildAttribute (BuiltIn.RequiresCapability.FullName, args)

/// Infers the capability of all callables in the compilation, adding the built-in Capability attribute to each
/// callable.
let InferCapabilities compilation =
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = CallGraph compilation
    let transformation = SyntaxTreeTransformation ()
    let callableCapability = callableDependentCapability (callables, graph)
    transformation.Namespaces <- {
        new NamespaceTransformation (transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability = SymbolResolution.TryGetRequiredCapability callable.Attributes |> isQsNull
                if isMissingCapability && isDeclaredInSourceFile callable
                then callableCapability callable |> toAttribute |> callable.AddAttribute
                else callable
    }
    transformation.OnCompilation compilation
