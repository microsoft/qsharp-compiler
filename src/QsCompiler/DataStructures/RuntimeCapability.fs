// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler

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

    let ofString name =
        match name with
        | "Opaque" -> Some Opaque
        | "Controlled" -> Some Controlled
        | "Transparent" -> Some Transparent
        | _ -> None

type ClassicalCapability =
    | Limited
    | Unlimited

module ClassicalCapability =
    [<CompiledName "Limited">]
    let limited = Limited

    [<CompiledName "Unlimited">]
    let unlimited = Unlimited

    let ofString name =
        match name with
        | "Limited" -> Some Limited
        | "Unlimited" -> Some Unlimited
        | _ -> None

type RuntimeCapability =
    {
        resultOpacity: ResultOpacity
        classical: ClassicalCapability
    }

    member capability.ResultOpacity = capability.resultOpacity

    member capability.Classical = capability.classical

module RuntimeCapability =
    [<CompiledName "Top">]
    let top = { resultOpacity = Transparent; classical = Unlimited }

    [<CompiledName "Bottom">]
    let bottom = { resultOpacity = Opaque; classical = Limited }

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
        Map [ "BasicExecution", { resultOpacity = Opaque; classical = Limited }
              "BasicQuantumFunctionality", { resultOpacity = Opaque; classical = Unlimited }
              "AdaptiveExecution", { resultOpacity = Controlled; classical = Limited }
              "BasicMeasurementFeedback", { resultOpacity = Controlled; classical = Unlimited }
              "FullComputation", { resultOpacity = Transparent; classical = Unlimited } ]

    [<CompiledName "Name">]
    let name capability =
        Map.tryFindKey (fun _ -> (=) capability) names

    [<CompiledName "FromString">]
    let ofString name = Map.tryFind name names

type RuntimeCapability with
    member capability.Name = RuntimeCapability.name capability |> Option.toObj

    static member Parse name =
        RuntimeCapability.ofString name |> Option.defaultValue Unchecked.defaultof<_>
