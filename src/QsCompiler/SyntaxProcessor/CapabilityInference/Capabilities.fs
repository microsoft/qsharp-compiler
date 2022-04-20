// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.Diagnostics
open Microsoft.Quantum.QsCompiler.SyntaxProcessing
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Utils
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

let analyzers = [ ResultAnalyzer.analyze; StatementAnalyzer.analyze; TypeAnalyzer.analyze ]

let analyzeScope scope (analyzer: Analyzer) =
    analyzer (fun transformation -> transformation.Statements.OnScope scope |> ignore)

/// Returns the offset of a nullable location.
let locationOffset = QsNullable<_>.Map (fun (l: QsLocation) -> l.Offset)

/// Returns the joined capability of the sequence of capabilities, or the default capability if the sequence is empty.
let joinCapabilities = Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

/// Returns a list of the names of global callables referenced in the scope, and the range of the reference relative to
/// the start of the specialization.
let globalReferences scope =
    let transformation = LocationTrackingTransformation TransformationOptions.NoRebuild
    let mutable references = []

    transformation.Expressions <-
        { new ExpressionTransformation(transformation, TransformationOptions.NoRebuild) with
            override this.OnTypedExpression expression =
                match expression.Expression with
                | Identifier (GlobalCallable name, _) ->
                    let range = QsNullable.Map2(+) transformation.Offset expression.Range
                    references <- (name, range) :: references
                | _ -> ()

                base.OnTypedExpression expression
        }

    transformation.Statements.OnScope scope |> ignore
    references

/// Returns diagnostic reasons for why a global callable reference is not supported.
let rec referenceReasons
    context
    (name: QsQualifiedName)
    (range: _ QsNullable)
    (header: SpecializationDeclarationHeader, impl)
    =
    let reason (header: SpecializationDeclarationHeader) diagnostic =
        match diagnostic.Diagnostic with
        | Error ErrorCode.UnsupportedResultComparison -> Some WarningCode.UnsupportedResultComparison
        | Error ErrorCode.ResultComparisonNotInOperationIf -> Some WarningCode.ResultComparisonNotInOperationIf
        | Error ErrorCode.ReturnInResultConditionedBlock -> Some WarningCode.ReturnInResultConditionedBlock
        | Error ErrorCode.SetInResultConditionedBlock -> Some WarningCode.SetInResultConditionedBlock
        | Error ErrorCode.UnsupportedCallableCapability -> Some WarningCode.UnsupportedCallableCapability
        | _ -> None
        |> Option.map (fun code ->
            let args =
                Seq.append
                    [
                        name.Name
                        header.Source.CodeFile
                        string (diagnostic.Range.Start.Line + 1)
                        string (diagnostic.Range.Start.Column + 1)
                    ]
                    diagnostic.Arguments

            range.ValueOr Range.Zero |> QsCompilerDiagnostic.Warning(code, args))

    match impl with
    | Provided (_, scope) ->
        diagnoseImpl false context scope
        |> Seq.map (fun diagnostic ->
            locationOffset header.Location
            |> QsNullable<_>.Map (fun offset -> { diagnostic with Range = offset + diagnostic.Range })
            |> QsNullable.defaultValue diagnostic)
        |> Seq.choose (reason header)
    | _ -> Seq.empty

/// Returns diagnostics for a reference to a global callable with the given name, based on its capability attribute and
/// the context's supported runtime capabilities.
and referenceDiagnostics includeReasons context (name, range) =
    match context.Globals.TryGetCallable name (context.Symbols.Parent.Namespace, context.Symbols.SourceFile) with
    | Found declaration ->
        let capability =
            (SymbolResolution.TryGetRequiredCapability declaration.Attributes).ValueOr RuntimeCapability.Base

        if context.Capability.Implies capability then
            Seq.empty
        else
            let reasons =
                if includeReasons then
                    context.Globals.ImportedSpecializations name |> Seq.collect (referenceReasons context name range)
                else
                    Seq.empty

            let error =
                ErrorCode.UnsupportedCallableCapability, [ name.Name; string capability; context.ProcessorArchitecture ]

            let diagnostic = QsCompilerDiagnostic.Error error (range.ValueOr Range.Zero)
            Seq.append (Seq.singleton diagnostic) reasons
    | _ -> Seq.empty

/// Returns all capability diagnostics for the scope. Ranges are relative to the start of the specialization.
and diagnoseImpl includeReasons context scope : QsCompilerDiagnostic seq =
    Seq.append
        (globalReferences scope |> Seq.collect (referenceDiagnostics includeReasons context))
        (Seq.collect (analyzeScope scope) analyzers |> Seq.choose (fun p -> p.Diagnostic context))

[<CompiledName "Diagnose">]
let diagnose context scope = diagnoseImpl true context scope

/// Returns true if the callable is an operation.
let isOperation callable =
    match callable.Kind with
    | Operation -> true
    | _ -> false

/// Returns true if the callable is declared in a source file in the current compilation, instead of a referenced
/// library.
let isDeclaredInSourceFile (callable: QsCallable) =
    QsNullable.isNull callable.Source.AssemblyFile

/// Given whether the specialization is part of an operation, returns its required capability based on its source code,
/// ignoring callable dependencies.
let specSourceCapability inOperation spec =
    match spec.Implementation with
    | Provided (_, scope) ->
        Seq.collect (analyzeScope scope) analyzers
        |> Seq.map (fun p -> p.Capability inOperation)
        |> joinCapabilities
    | _ -> RuntimeCapability.Base

/// Returns the required runtime capability of the callable based on its source code, ignoring callable dependencies.
let callableSourceCapability callable =
    callable.Specializations
    |> Seq.map (isOperation callable |> specSourceCapability)
    |> joinCapabilities

/// A mapping from callable name to runtime capability based on callable source code patterns and cycles the callable
/// is a member of, but not other dependencies.
let sourceCycleCapabilities (callables: ImmutableDictionary<_, _>) (graph: CallGraph) =
    let initialCapabilities =
        callables
        |> Seq.filter (fun item -> isDeclaredInSourceFile item.Value)
        |> fun items -> items.ToDictionary((fun item -> item.Key), (fun item -> callableSourceCapability item.Value))

    let sourceCycles =
        graph.GetCallCycles()
        |> Seq.filter (
            Seq.exists (fun node ->
                callables.TryGetValue node.CallableName |> tryOption |> Option.exists isDeclaredInSourceFile)
        )

    for cycle in sourceCycles do
        let cycleCapability =
            cycle
            |> Seq.choose (fun node -> callables.TryGetValue node.CallableName |> tryOption)
            |> Seq.map callableSourceCapability
            |> joinCapabilities

        for node in cycle do
            initialCapabilities[node.CallableName] <- joinCapabilities [ initialCapabilities[node.CallableName]
                                                                         cycleCapability ]

    initialCapabilities

/// Returns the required capability of the callable based on its capability attribute if one is present. If no attribute
/// is present and the callable is not defined in a reference, returns the capability based on its source code and
/// callable dependencies. Otherwise, returns the base capability.
///
/// Partially applying the first argument creates a memoized function that caches computed runtime capabilities by
/// callable name. The memoized function is not thread-safe.
let callableDependentCapability (callables: ImmutableDictionary<_, _>) (graph: CallGraph) =
    let initialCapabilities = sourceCycleCapabilities callables graph
    let cache = Dictionary()

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
        |> Seq.choose (fun name -> callables.TryGetValue name |> tryOption)
        |> Seq.map (cachedCapability visited)
        |> joinCapabilities

    // The capability of a callable based on its initial capability and the capability of all dependencies.
    and callableCapability visited (callable: QsCallable) =
        (SymbolResolution.TryGetRequiredCapability callable.Attributes)
            .ValueOrApply(fun () ->
                if isDeclaredInSourceFile callable then
                    [
                        initialCapabilities.TryGetValue callable.FullName
                        |> tryOption
                        |> Option.defaultValue RuntimeCapability.Base

                        dependentCapability visited callable.FullName
                    ]
                    |> joinCapabilities
                else
                    RuntimeCapability.Base)

    // Tries to retrieve the capability of the callable from the cache first; otherwise, computes the capability and
    // saves it in the cache.
    and cachedCapability visited (callable: QsCallable) =
        tryOption (cache.TryGetValue callable.FullName)
        |> Option.defaultWith (fun () ->
            let capability = callableCapability visited callable
            cache[callable.FullName] <- capability
            capability)

    cachedCapability Set.empty

/// Returns the attribute for the inferred runtime capability.
let toAttribute capability =
    let args = AttributeUtils.StringArguments(string capability, "Inferred automatically by the compiler.")
    AttributeUtils.BuildAttribute(BuiltIn.RequiresCapability.FullName, args)

[<CompiledName "InferAttributes">]
let inferAttributes compilation =
    let callables = GlobalCallableResolutions compilation.Namespaces
    let graph = CallGraph compilation
    let transformation = SyntaxTreeTransformation()
    let callableCapability = callableDependentCapability callables graph

    transformation.Namespaces <-
        { new NamespaceTransformation(transformation) with
            override this.OnCallableDeclaration callable =
                let isMissingCapability =
                    SymbolResolution.TryGetRequiredCapability callable.Attributes |> QsNullable.isNull

                if isMissingCapability && isDeclaredInSourceFile callable then
                    callableCapability callable |> toAttribute |> callable.AddAttribute
                else
                    callable
        }

    transformation.OnCompilation compilation
