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
type ClassicalCapability

module ClassicalCapability =
    /// No classical capabilities are supported.
    [<CompiledName "Empty">]
    val empty: ClassicalCapability

    /// Computations involving boolean and integer values are supported.
    [<CompiledName "Integral">]
    val integral: ClassicalCapability

    /// All classical capabilities are supported.
    [<CompiledName "Full">]
    val full: ClassicalCapability

    val internal ofName: name: string -> ClassicalCapability option

/// Describes which program capabilities are supported at runtime.
[<Sealed>]
type RuntimeCapability =
    /// The capabilities supported for measurement results.
    member ResultOpacity: ResultOpacity

    /// The capabilities supported for classical computation.
    member Classical: ClassicalCapability

    /// <summary>
    /// The name of this capability, or <c>null</c> if it has none.
    /// </summary>
    [<MaybeNull>]
    member Name: string

    /// Opaque result opacity and empty classical capability.
    static member BasicExecution: RuntimeCapability

    /// Transparent result opacity and integral classical capability.
    static member AdaptiveExecution: RuntimeCapability

    /// Opaque result opacity and full classical capability.
    static member BasicQuantumFunctionality: RuntimeCapability

    /// Controlled result opacity and full classical capability.
    static member BasicMeasurementFeedback: RuntimeCapability

    /// Transparent result opacity and full classical capability.
    static member FullComputation: RuntimeCapability

    /// <summary>
    /// Parses a runtime capability name or returns <c>null</c> if <paramref name="name"/> does not correspond to a
    /// runtime capability.
    /// </summary>
    [<return: MaybeNull>]
    static member Parse: name: string -> RuntimeCapability

module RuntimeCapability =
    /// The runtime capability that subsumes all other capabilities.
    [<CompiledName "Top">]
    val top: RuntimeCapability

    /// The runtime capability that is subsumed by all other capabilities.
    [<CompiledName "Bottom">]
    val bottom: RuntimeCapability

    /// <summary>
    /// <c>true</c> if <paramref name="c1"/> has all capabilities of <paramref name="c2"/> or more.
    /// </summary>
    [<CompiledName "Subsumes">]
    val subsumes: c1: RuntimeCapability -> c2: RuntimeCapability -> bool

    /// <summary>
    /// Creates a runtime capability that subsumes both <paramref name="c1"/> and <paramref name="c2"/>.
    /// </summary>
    [<CompiledName "Merge">]
    val merge: c1: RuntimeCapability -> c2: RuntimeCapability -> RuntimeCapability

    /// Updates the result opacity of the runtime capability.
    [<CompiledName "WithResultOpacity">]
    val withResultOpacity: opacity: ResultOpacity -> capability: RuntimeCapability -> RuntimeCapability

    /// Updates the classical capability of the runtime capability.
    [<CompiledName "WithClassical">]
    val withClassical: classical: ClassicalCapability -> capability: RuntimeCapability -> RuntimeCapability

    /// <summary>
    /// The name of the capability, or <c>None</c> if it has none.
    /// </summary>
    [<CompiledName "Name">]
    val name: capability: RuntimeCapability -> string option

    /// <summary>
    /// Creates a runtime capability from its name or returns <c>None</c> if <paramref name="name"/> does not correspond
    /// to a runtime capability.
    /// </summary>
    [<CompiledName "FromName">]
    val ofName: name: string -> RuntimeCapability option
