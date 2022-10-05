// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type Target =
    {
        _Name: string
        _Capability: TargetCapability
    }

    member target.Name = target._Name

    member target.Capability = target._Capability

module Target =
    [<CompiledName "Create">]
    let create name capability =
        { _Name = name; _Capability = capability }

type Pattern = { Capability: TargetCapability; Diagnose: Target -> QsCompilerDiagnostic option }

module Pattern =
    let concat patterns =
        Seq.fold
            (fun capability pattern -> TargetCapability.merge pattern.Capability capability)
            TargetCapability.bottom
            patterns

type Analyzer<'Subject> = 'Subject -> Pattern seq

module Analyzer =
    let concat (analyzers: Analyzer<_> seq) subject = Seq.collect ((|>) subject) analyzers

type LocatingTransformation(options) =
    inherit SyntaxTreeTransformation(options)

    let mutable absolute = Null
    let mutable relative = Null

    member _.Offset =
        match absolute, relative with
        | Value a, Value r -> a + r |> Value
        | Value a, Null -> Value a
        | Null, _ -> Null

    override _.OnAbsoluteLocation location =
        absolute <- location |> QsNullable<_>.Map (fun l -> l.Offset)
        relative <- Null
        location

    override _.OnRelativeLocation location =
        relative <- location |> QsNullable<_>.Map (fun l -> l.Offset)
        location

module ContextRef =
    let local value (context: _ ref) =
        let old = context.Value
        context.Value <- value

        { new IDisposable with
            member _.Dispose() = context.Value <- old
        }
