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

    val internal ofString: name: string -> ResultOpacity option

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

    val internal ofString: name: string -> ClassicalCapability option

[<Sealed>]
type RuntimeCapability =
    interface IComparable

    member ResultOpacity: ResultOpacity

    member Classical: ClassicalCapability

    [<MaybeNull>]
    member Name: string

    [<return: MaybeNull>]
    static member Parse: name: string -> RuntimeCapability

module RuntimeCapability =
    [<CompiledName "Top">]
    val top: RuntimeCapability

    [<CompiledName "Bottom">]
    val bottom: RuntimeCapability

    [<CompiledName "Merge">]
    val merge: c1: RuntimeCapability -> c2: RuntimeCapability -> RuntimeCapability

    [<CompiledName "WithResultOpacity">]
    val withResultOpacity: opacity: ResultOpacity -> capability: RuntimeCapability -> RuntimeCapability

    [<CompiledName "WithClassical">]
    val withClassical: classical: ClassicalCapability -> capability: RuntimeCapability -> RuntimeCapability

    [<CompiledName "Name">]
    val name: capability: RuntimeCapability -> string option

    [<CompiledName "FromString">]
    val ofString: name: string -> RuntimeCapability option
