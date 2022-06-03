// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System.Diagnostics.CodeAnalysis

/// Describes whether and how measurement results may be inspected at runtime.
type ResultOpacity

module ResultOpacity =
    /// Measurement results can never be inspected.
    [<CompiledName "Opaque">]
    val opaque: ResultOpacity

    /// Measurement results can be compared for equality only in if-statement conditional expressions in operations.
    /// The block of an if-statement that depends on a result cannot contain set statements for mutable variables
    /// declared outside the block, or return statements.
    [<CompiledName "Controlled">]
    val controlled: ResultOpacity

    /// Measurement results can always be inspected.
    [<CompiledName "Transparent">]
    val transparent: ResultOpacity

    val internal ofName: name: string -> ResultOpacity option

/// Describes what kinds of classical computations may be performed at runtime.
type ClassicalCompute

module ClassicalCompute =
    /// No classical capabilities are supported.
    [<CompiledName "Empty">]
    val empty: ClassicalCompute

    /// Computations involving boolean and integer values are supported.
    [<CompiledName "Integral">]
    val integral: ClassicalCompute

    /// All classical capabilities are supported.
    [<CompiledName "Full">]
    val full: ClassicalCompute

    val internal ofName: name: string -> ClassicalCompute option

/// Describes the capability of a compilation target.
[<Sealed>]
type TargetCapability =
    /// The capabilities supported for measurement results.
    member ResultOpacity: ResultOpacity

    /// The capabilities supported for classical computation.
    member ClassicalCompute: ClassicalCompute

    /// <summary>
    /// The name of this capability, or <c>null</c> if it has none.
    /// </summary>
    [<MaybeNull>]
    member Name: string

    /// <summary>
    /// Parses a capability name or returns <c>null</c> if <paramref name="name"/> does not correspond to a capability.
    /// </summary>
    [<return: MaybeNull>]
    static member TryParse: name: string -> TargetCapability

module TargetCapability =
    /// The capability that subsumes all other capabilities.
    [<CompiledName "Top">]
    val top: TargetCapability

    /// The capability that is subsumed by all other capabilities.
    [<CompiledName "Bottom">]
    val bottom: TargetCapability

    /// Opaque result opacity and empty classical capability.
    [<CompiledName "BasicExecution">]
    val basicExecution: TargetCapability

    /// Transparent result opacity and integral classical capability.
    [<CompiledName "AdaptiveExecution">]
    val adaptiveExecution: TargetCapability

    /// Opaque result opacity and full classical capability.
    [<CompiledName "BasicQuantumFunctionality">]
    val basicQuantumFunctionality: TargetCapability

    /// Controlled result opacity and full classical capability.
    [<CompiledName "BasicMeasurementFeedback">]
    val basicMeasurementFeedback: TargetCapability

    /// Transparent result opacity and full classical capability.
    [<CompiledName "FullComputation">]
    val fullComputation: TargetCapability

    /// <summary>
    /// <c>true</c> if <paramref name="c1"/> has all capabilities of <paramref name="c2"/> or more.
    /// </summary>
    [<CompiledName "Subsumes">]
    val subsumes: c1: TargetCapability -> c2: TargetCapability -> bool

    /// <summary>
    /// Creates a capability that:
    ///
    /// <list>
    /// <item>Subsumes both <paramref name="c1"/> and <paramref name="c2"/>;</item>
    /// <item>Minimizes the size of the set of capabilities that it subsumes.</item>
    /// </list>
    /// </summary>
    [<CompiledName "Merge">]
    val merge: c1: TargetCapability -> c2: TargetCapability -> TargetCapability

    /// Updates the result opacity capability.
    [<CompiledName "WithResultOpacity">]
    val withResultOpacity: opacity: ResultOpacity -> capability: TargetCapability -> TargetCapability

    /// Updates the classical compute capability.
    [<CompiledName "WithClassicalCompute">]
    val withClassicalCompute: classical: ClassicalCompute -> capability: TargetCapability -> TargetCapability

    /// <summary>
    /// The name of the capability, or <c>None</c> if it has none.
    /// </summary>
    [<CompiledName "Name">]
    val name: capability: TargetCapability -> string option

    /// <summary>
    /// Creates a capability from its name or returns <c>None</c> if <paramref name="name"/> does not correspond to a
    /// capability.
    /// </summary>
    [<CompiledName "FromName">]
    val ofName: name: string -> TargetCapability option
