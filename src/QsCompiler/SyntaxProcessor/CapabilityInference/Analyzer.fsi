// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.Core

[<Sealed>]
type Target =
    member internal Capability: RuntimeCapability

    member internal Architecture: string

module Target =
    [<CompiledName "Create">]
    val create: capability: RuntimeCapability -> architecture: string -> Target

type internal 'props Pattern =
    { Capability: RuntimeCapability
      Diagnose: Target -> QsCompilerDiagnostic option
      Properties: 'props }

module internal Pattern =
    val discard: 'props Pattern -> unit Pattern

    val max: 'props Pattern seq -> RuntimeCapability

type internal Analyzer<'subject, 'props> = 'subject -> 'props Pattern seq

module internal Analyzer =
    val concat: analyzers: Analyzer<'subject, 'props> seq -> Analyzer<'subject, 'props>

type internal LocatingTransformation =
    inherit SyntaxTreeTransformation

    new: options: TransformationOptions -> LocatingTransformation

    member Offset: Position QsNullable

module ContextRef =
    val local: value: 'a -> context: 'a ref -> IDisposable
