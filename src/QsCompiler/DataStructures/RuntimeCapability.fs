﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

#nowarn "44" // RuntimeCapabilities is deprecated.

open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants
open System
open System.Runtime.CompilerServices

/// The runtime capabilities supported by a quantum processor.
[<NoComparison>]
type RuntimeCapability =
    /// Measurement results cannot be compared for equality.
    | BasicQuantumFunctionality

    /// Measurement results can be compared for equality only in if-statement conditional expressions in operations.
    /// The block of an if-statement that depends on a result cannot contain set statements for mutable variables
    /// declared outside the block, or return statements.
    | BasicMeasurementFeedback

    /// No runtime restrictions. Any Q# program can be executed.
    | FullComputation

    /// Returns true if having this runtime capability also implies having the given runtime capability.
    member x.Implies y =
        match x, y with
        | BasicMeasurementFeedback, BasicQuantumFunctionality
        | FullComputation, _ -> true
        | _ -> x = y

    /// Returns a runtime capability that implies both given capabilities.
    static member Combine x y =
        match x, y with
        | BasicQuantumFunctionality, other
        | other, BasicQuantumFunctionality -> other
        | BasicMeasurementFeedback, BasicMeasurementFeedback -> BasicMeasurementFeedback
        | FullComputation, _
        | _, FullComputation -> FullComputation

    /// The base runtime capability is the identity element when combined with another capability. It is implied by
    /// every other capability.
    static member Base = BasicQuantumFunctionality

    /// Returns true if both runtime capabilities are equal.
    static member op_Equality(a: RuntimeCapability, b: RuntimeCapability) = a = b

    /// Parses the string as a runtime capability.
    static member TryParse value =
        // TODO: RELEASE 2021-04: Remove parsing for "QPRGen0", "QPRGen1", and "Unknown".
        match value with
        | "BasicQuantumFunctionality"
        | "QPRGen0" -> Value BasicQuantumFunctionality
        | "BasicMeasurementFeedback"
        | "QPRGen1" -> Value BasicMeasurementFeedback
        | "FullComputation"
        | "Unknown" -> Value FullComputation
        | _ -> Null

    member this.Name =
        match this with
        | BasicQuantumFunctionality -> "BasicQuantumFunctionality"
        | BasicMeasurementFeedback -> "BasicMeasurementFeedback"
        | FullComputation -> "FullComputation"

// TODO: RELEASE 2021-04: Remove RuntimeCapabilitiesExtensions.
[<Extension>]
[<Obsolete "Use Microsoft.Quantum.QsCompiler.RuntimeCapability.">]
module RuntimeCapabilitiesExtensions =
    [<Extension>]
    [<Obsolete "Use Microsoft.Quantum.QsCompiler.RuntimeCapability.">]
    let ToCapability capabilities =
        match capabilities with
        | RuntimeCapabilities.QPRGen0 -> BasicQuantumFunctionality
        | RuntimeCapabilities.QPRGen1 -> BasicMeasurementFeedback
        | _ -> FullComputation
