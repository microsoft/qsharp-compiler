// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference.Capabilities

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.DependencyAnalysis
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Utils
open System.Collections.Generic
open System.Collections.Immutable
open System.Linq

[<CompiledName "Diagnose">]
let diagnose target nsManager graph callable =
    let env = { CallableKind = callable.Kind }

    fun (t: SyntaxTreeTransformation) -> t.Namespaces.OnCallableDeclaration callable |> ignore
    |> CallAnalyzer.analyzeAllShallow nsManager graph (CallGraphNode callable.FullName) env
    |> Seq.collect (fun p -> Seq.append (p.Diagnose target |> Option.toList) (p.Explain(target, nsManager, graph)))

/// Returns true if the callable is declared in a source file in the current compilation, instead of a referenced
/// library.
let isDeclaredInSourceFile (callable: QsCallable) =
    QsNullable.isNull callable.Source.AssemblyFile

/// Returns the joined capability of the sequence of capabilities, or the default capability if the sequence is empty.
let joinCapabilities = Seq.fold RuntimeCapability.Combine RuntimeCapability.Base

/// Returns the required runtime capability of the callable based on its source code, ignoring callable dependencies.
let callableSourceCapability callable =
    let env = { CallableKind = callable.Kind }

    CallAnalyzer.analyzeSyntax env (fun t -> t.Namespaces.OnCallableDeclaration callable |> ignore)
    |> Seq.map (fun p -> p.Capability)
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
