// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System
open System.Diagnostics.CodeAnalysis

[<Sealed>]
type ResultOpacity =
    interface IComparable

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

[<Sealed>]
type ClassicalCapability =
    interface IComparable

module ClassicalCapability =
    [<CompiledName "Empty">]
    val empty: ClassicalCapability

    [<CompiledName "Integral">]
    val integral: ClassicalCapability

    [<CompiledName "Full">]
    val full: ClassicalCapability

    val internal ofName: name: string -> ClassicalCapability option

[<NoComparison>]
[<Sealed>]
type RuntimeCapability =
    member ResultOpacity: ResultOpacity

    member Classical: ClassicalCapability

    [<MaybeNull>]
    member Name: string

    static member BasicExecution: RuntimeCapability

    static member AdaptiveExecution: RuntimeCapability

    static member BasicQuantumFunctionality: RuntimeCapability

    static member BasicMeasurementFeedback: RuntimeCapability

    static member FullComputation: RuntimeCapability

    [<return: MaybeNull>]
    static member Parse: name: string -> RuntimeCapability

module RuntimeCapability =
    [<CompiledName "Top">]
    val top: RuntimeCapability

    [<CompiledName "Bottom">]
    val bottom: RuntimeCapability

    [<CompiledName "Subsumes">]
    val subsumes: c1: RuntimeCapability -> c2: RuntimeCapability -> bool

    [<CompiledName "Merge">]
    val merge: c1: RuntimeCapability -> c2: RuntimeCapability -> RuntimeCapability

    [<CompiledName "WithResultOpacity">]
    val withResultOpacity: opacity: ResultOpacity -> capability: RuntimeCapability -> RuntimeCapability

    [<CompiledName "WithClassical">]
    val withClassical: classical: ClassicalCapability -> capability: RuntimeCapability -> RuntimeCapability

    [<CompiledName "Name">]
    val name: capability: RuntimeCapability -> string option

    [<CompiledName "FromName">]
    val ofName: name: string -> RuntimeCapability option
