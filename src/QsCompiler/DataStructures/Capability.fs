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
        _ResultOpacity: ResultOpacity
        _ClassicalCompute: ClassicalCompute
    }

    member capability.ResultOpacity = capability._ResultOpacity

    member capability.ClassicalCompute = capability._ClassicalCompute

module TargetCapability =
    [<CompiledName "Top">]
    let top = { _ResultOpacity = Transparent; _ClassicalCompute = Full }

    [<CompiledName "Bottom">]
    let bottom = { _ResultOpacity = Opaque; _ClassicalCompute = Empty }

    [<CompiledName "BasicExecution">]
    let basicExecution = { _ResultOpacity = Opaque; _ClassicalCompute = Empty }

    [<CompiledName "AdaptiveExecution">]
    let adaptiveExecution = { _ResultOpacity = Transparent; _ClassicalCompute = Integral }

    [<CompiledName "BasicQuantumFunctionality">]
    let basicQuantumFunctionality = { _ResultOpacity = Opaque; _ClassicalCompute = Full }

    [<CompiledName "BasicMeasurementFeedback">]
    let basicMeasurementFeedback = { _ResultOpacity = Controlled; _ClassicalCompute = Full }

    [<CompiledName "FullComputation">]
    let fullComputation = { _ResultOpacity = Transparent; _ClassicalCompute = Full }

    [<CompiledName "Subsumes">]
    let subsumes (c1: TargetCapability) (c2: TargetCapability) =
        c1.ResultOpacity >= c2.ResultOpacity
        && ClassicalCompute.subsumes c1.ClassicalCompute c2.ClassicalCompute

    [<CompiledName "Merge">]
    let merge (c1: TargetCapability) (c2: TargetCapability) =
        {
            _ResultOpacity = max c1.ResultOpacity c2.ResultOpacity
            _ClassicalCompute = ClassicalCompute.merge c1.ClassicalCompute c2.ClassicalCompute
        }

    [<CompiledName "WithResultOpacity">]
    let withResultOpacity opacity capability =
        { capability with _ResultOpacity = opacity }

    [<CompiledName "WithClassicalCompute">]
    let withClassicalCompute classical capability =
        { capability with _ClassicalCompute = classical }

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
