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

    let ofString name =
        match name with
        | "Empty" -> Some Empty
        | "Integral" -> Some Integral
        | "Full" -> Some Full
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
    let top = { resultOpacity = Transparent; classical = Full }

    [<CompiledName "Bottom">]
    let bottom = { resultOpacity = Opaque; classical = Empty }

    [<CompiledName "Merge">]
    let merge c1 c2 =
        { resultOpacity = max c1.resultOpacity c2.resultOpacity; classical = max c1.classical c2.classical }

    [<CompiledName "WithResultOpacity">]
    let withResultOpacity opacity capability =
        { capability with resultOpacity = opacity }

    [<CompiledName "WithClassical">]
    let withClassical classical capability =
        { capability with classical = classical }

    // TODO: Need to know the target architecture to choose between Empty and Integral.
    let names =
        Map [ "BasicExecution", { resultOpacity = Opaque; classical = Empty }
              "BasicQuantumFunctionality", { resultOpacity = Opaque; classical = Full }
              "AdaptiveExecution", { resultOpacity = Controlled; classical = Empty }
              "BasicMeasurementFeedback", { resultOpacity = Controlled; classical = Full }
              "FullComputation", { resultOpacity = Transparent; classical = Full } ]

    [<CompiledName "Name">]
    let name capability =
        Map.tryFindKey (fun _ -> (=) capability) names

    [<CompiledName "FromString">]
    let ofString name = Map.tryFind name names

type RuntimeCapability with
    member capability.Name = RuntimeCapability.name capability |> Option.toObj

    static member Parse name =
        RuntimeCapability.ofString name |> Option.defaultValue Unchecked.defaultof<_>
