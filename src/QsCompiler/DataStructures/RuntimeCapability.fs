// Copyright (c) Microsoft Corporation.
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

    /// Measurement results cannot be compared for equality,
    /// and language constructs are limited such that the program can be compiled into Base Profile QIR.
    | BasicExecution

    /// Language constructs are limited such that the program can be compiled into
    /// a QIR profile that supports adaptive algorithms involving measurement results, integers and booleans.
    | AdaptiveExecution

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
        | BasicQuantumFunctionality, BasicExecution
        | BasicMeasurementFeedback, BasicExecution
        | BasicMeasurementFeedback, BasicQuantumFunctionality
        | AdaptiveExecution, BasicExecution
        | AdaptiveExecution, BasicMeasurementFeedback // FIXME: REMOVE
        | AdaptiveExecution, BasicQuantumFunctionality // FIXME: REMOVE
        | FullComputation, _ -> true
        | _ -> x = y

    /// Returns a runtime capability that implies both given capabilities.
    static member Combine x y =
        match x, y with
        | BasicExecution, other
        | other, BasicExecution -> other
        | AdaptiveExecution, _
        | _, AdaptiveExecution -> FullComputation
        | BasicQuantumFunctionality, other
        | other, BasicQuantumFunctionality -> other
        | BasicMeasurementFeedback, other
        | other, BasicMeasurementFeedback -> other
        | FullComputation, FullComputation -> FullComputation

    /// The base runtime capability is the identity element when combined with another capability. It is implied by
    /// every other capability.
    static member Base = BasicExecution

    /// Returns true if both runtime capabilities are equal.
    static member op_Equality(a: RuntimeCapability, b: RuntimeCapability) = a = b

    /// Parses the string as a runtime capability.
    static member TryParse value =
        // TODO: RELEASE 2021-04: Remove parsing for "QPRGen0", "QPRGen1", and "Unknown".
        match value with
        | "BasicExecution" -> Value BasicExecution
        | "AdaptiveExecution" -> Value AdaptiveExecution
        | "BasicQuantumFunctionality"
        | "QPRGen0" -> Value BasicQuantumFunctionality
        | "BasicMeasurementFeedback"
        | "QPRGen1" -> Value BasicMeasurementFeedback
        | "FullComputation"
        | "Unknown" -> Value FullComputation
        | _ -> Null

    member this.Name =
        match this with
        | BasicExecution -> "BasicExecution"
        | AdaptiveExecution -> "AdaptiveExecution"
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
