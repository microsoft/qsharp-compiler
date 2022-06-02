// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

open System

[<CustomComparison>]
[<CustomEquality>]
type CaseInsensitive =
    | CaseInsensitive of string

    member ci.Original =
        let (CaseInsensitive s) = ci
        s

    override ci.Equals obj = (ci :> IComparable).CompareTo obj = 0

    override ci.GetHashCode() =
        StringComparer.InvariantCultureIgnoreCase.GetHashCode ci.Original

    interface IComparable with
        member ci.CompareTo obj =
            match obj with
            | :? CaseInsensitive as other ->
                String.Compare(ci.Original, other.Original, StringComparison.InvariantCultureIgnoreCase)
            | _ -> ArgumentException("Type mismatch.", nameof obj) |> raise

module CaseInsensitive =
    let original (CaseInsensitive s) = s

type ResultOpacity =
    | Opaque
    | Controlled
    | Transparent

module ResultOpacity =
    [<CompiledName "Opaque">]
    let opaque = Opaque

    [<CompiledName "Controlled">]
    let controlled = Controlled

    [<CompiledName "Transparent">]
    let transparent = Transparent

    let names =
        Map [ CaseInsensitive "Opaque", Opaque
              CaseInsensitive "Controlled", Controlled
              CaseInsensitive "Transparent", Transparent ]

    let ofName name =
        Map.tryFind (CaseInsensitive name) names

type ClassicalCapability =
    | Empty
    | Integral
    | Full

module ClassicalCapability =
    [<CompiledName "Empty">]
    let empty = Empty

    [<CompiledName "Integral">]
    let integral = Integral

    [<CompiledName "Full">]
    let full = Full

    let names =
        Map [ CaseInsensitive "Empty", Empty
              CaseInsensitive "Integral", Integral
              CaseInsensitive "Full", Full ]

    let ofName name =
        Map.tryFind (CaseInsensitive name) names

[<NoComparison>]
type TargetCapability =
    {
        resultOpacity: ResultOpacity
        classical: ClassicalCapability
    }

    member capability.ResultOpacity = capability.resultOpacity

    member capability.Classical = capability.classical

module TargetCapability =
    [<CompiledName "Top">]
    let top = { resultOpacity = Transparent; classical = Full }

    [<CompiledName "Bottom">]
    let bottom = { resultOpacity = Opaque; classical = Empty }

    [<CompiledName "BasicExecution">]
    let basicExecution = { resultOpacity = Opaque; classical = Empty }

    [<CompiledName "AdaptiveExecution">]
    let adaptiveExecution = { resultOpacity = Transparent; classical = Integral }

    [<CompiledName "BasicQuantumFunctionality">]
    let basicQuantumFunctionality = { resultOpacity = Opaque; classical = Full }

    [<CompiledName "BasicMeasurementFeedback">]
    let basicMeasurementFeedback = { resultOpacity = Controlled; classical = Full }

    [<CompiledName "FullComputation">]
    let fullComputation = { resultOpacity = Transparent; classical = Full }

    [<CompiledName "Subsumes">]
    let subsumes c1 c2 =
        c1.resultOpacity >= c2.resultOpacity && c1.classical >= c2.classical

    [<CompiledName "Merge">]
    let merge c1 c2 =
        { resultOpacity = max c1.resultOpacity c2.resultOpacity; classical = max c1.classical c2.classical }

    [<CompiledName "WithResultOpacity">]
    let withResultOpacity opacity capability =
        { capability with resultOpacity = opacity }

    [<CompiledName "WithClassical">]
    let withClassical classical capability =
        { capability with classical = classical }

    let names =
        Map [ CaseInsensitive "BasicExecution", basicExecution
              CaseInsensitive "AdaptiveExecution", adaptiveExecution
              CaseInsensitive "BasicQuantumFunctionality", basicQuantumFunctionality
              CaseInsensitive "BasicMeasurementFeedback", basicMeasurementFeedback
              CaseInsensitive "FullComputation", fullComputation ]

    [<CompiledName "Name">]
    let name capability =
        Map.tryFindKey (fun _ -> (=) capability) names |> Option.map CaseInsensitive.original

    [<CompiledName "FromName">]
    let ofName name =
        Map.tryFind (CaseInsensitive name) names

type TargetCapability with
    member capability.Name = TargetCapability.name capability |> Option.toObj

    static member Parse name =
        TargetCapability.ofName name |> Option.defaultValue Unchecked.defaultof<_>
