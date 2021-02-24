// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing

#nowarn "44" // ScopeContext.IsInIfCondition is deprecated.

open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.Quantum.QsCompiler.SyntaxTree
open System

/// The context used for symbol resolution and type checking within the scope of a callable.
type ScopeContext =
    // TODO: RELEASE 2021-04: Remove IsInIfCondition and WithinIfCondition.
    {
        /// The namespace manager for global symbols.
        Globals: NamespaceManager

        /// The symbol tracker for the parent callable.
        Symbols: SymbolTracker

        Inference: InferenceContext

        /// True if the parent callable for the current scope is an operation.
        IsInOperation: bool

        /// True if the current expression is contained within the condition of an if- or elif-statement.
        [<Obsolete>]
        IsInIfCondition: bool

        /// The return type of the parent callable for the current scope.
        ReturnType: ResolvedType

        /// The runtime capability of the compilation unit.
        Capability: RuntimeCapability

        /// The name of the processor architecture for the compilation unit.
        ProcessorArchitecture: string
    }

    /// <summary>
    /// Creates a scope context for the specialization.
    ///
    /// The symbol tracker in the context does not make a copy of the given namespace manager. Instead, it throws an
    /// <see cref="InvalidOperationException"/> if the namespace manager has been modified (i.e. the version number of
    /// the namespace manager has changed).
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown if the given namespace manager does not contain all resolutions or if the specialization's parent does
    /// not exist in the given namespace manager.
    /// </exception>
    static member Create (nsManager: NamespaceManager)
                         capability
                         processorArchitecture
                         (spec: SpecializationDeclarationHeader)
                         =
        match nsManager.TryGetCallable spec.Parent (spec.Parent.Namespace, Source.assemblyOrCodeFile spec.Source) with
        | Found declaration ->
            let symbolTracker = SymbolTracker(nsManager, Source.assemblyOrCodeFile spec.Source, spec.Parent)

            {
                Globals = nsManager
                Symbols = symbolTracker
                Inference = InferenceContext symbolTracker
                IsInOperation = declaration.Kind = Operation
                IsInIfCondition = false
                ReturnType = StripPositionInfo.Apply declaration.Signature.ReturnType
                Capability = capability
                ProcessorArchitecture = processorArchitecture
            }
        | _ -> raise <| ArgumentException "The specialization's parent callable does not exist."

    /// Returns a new scope context for an expression that is contained within the condition of an if- or
    /// elif-statement.
    [<Obsolete>]
    member this.WithinIfCondition = { this with IsInIfCondition = true }
