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

[<NoComparison>]
type ClassicalCompute =
    | Empty
    | Integral
    | Full

module ClassicalCompute =
    [<CompiledName "Empty">]
    let empty = Empty

    [<CompiledName "Integral">]
    let integral = Integral

    [<CompiledName "Full">]
    let full = Full

    let subsumes c1 c2 =
        match c1, c2 with
        | Full, _
        | Integral, Integral
        | _, Empty -> true
        | _, Full
        | Empty, _ -> false

    let merge c1 c2 =
        match c1, c2 with
        | Full, _
        | _, Full -> Full
        | Integral, Integral -> Integral
        | c, Empty
        | Empty, c -> c

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
        classicalCompute: ClassicalCompute
    }

    member capability.ResultOpacity = capability.resultOpacity

    member capability.ClassicalCompute = capability.classicalCompute

module TargetCapability =
    [<CompiledName "Top">]
    let top = { resultOpacity = Transparent; classicalCompute = Full }

    [<CompiledName "Bottom">]
    let bottom = { resultOpacity = Opaque; classicalCompute = Empty }

    [<CompiledName "BasicExecution">]
    let basicExecution = { resultOpacity = Opaque; classicalCompute = Empty }

    [<CompiledName "AdaptiveExecution">]
    let adaptiveExecution = { resultOpacity = Transparent; classicalCompute = Integral }

    [<CompiledName "BasicQuantumFunctionality">]
    let basicQuantumFunctionality = { resultOpacity = Opaque; classicalCompute = Full }

    [<CompiledName "BasicMeasurementFeedback">]
    let basicMeasurementFeedback = { resultOpacity = Controlled; classicalCompute = Full }

    [<CompiledName "FullComputation">]
    let fullComputation = { resultOpacity = Transparent; classicalCompute = Full }

    [<CompiledName "Subsumes">]
    let subsumes c1 c2 =
        c1.resultOpacity >= c2.resultOpacity
        && ClassicalCompute.subsumes c1.classicalCompute c2.classicalCompute

    [<CompiledName "Merge">]
    let merge c1 c2 =
        {
            resultOpacity = max c1.resultOpacity c2.resultOpacity
            classicalCompute = ClassicalCompute.merge c1.classicalCompute c2.classicalCompute
        }

    [<CompiledName "WithResultOpacity">]
    let withResultOpacity opacity capability =
        { capability with resultOpacity = opacity }

    [<CompiledName "WithClassicalCompute">]
    let withClassicalCompute classical capability =
        { capability with classicalCompute = classical }

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

    static member TryParse name =
        TargetCapability.ofName name |> Option.defaultValue Unchecked.defaultof<_>
