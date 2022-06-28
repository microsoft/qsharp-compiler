﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.SyntaxProcessing.CapabilityInference

open System
open Microsoft.Quantum.QsCompiler
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.Transformations.Core

/// A target architecture.
[<Sealed>]
type Target =
    member internal Name: string

    member internal Capability: TargetCapability

module Target =
    /// Creates a target architecture.
    [<CompiledName "Create">]
    val create: name: string -> capability: TargetCapability -> Target

type internal 'props Pattern =
    {
        Capability: TargetCapability
        Diagnose: Target -> QsCompilerDiagnostic option
        // TODO: Remove the additional properties as part of https://github.com/microsoft/qsharp-compiler/issues/1448.
        Properties: 'props
    }

module internal Pattern =
    val discard: 'props Pattern -> unit Pattern

    val concat: 'props Pattern seq -> TargetCapability

type internal Analyzer<'subject, 'props> = 'subject -> 'props Pattern seq

module internal Analyzer =
    val concat: analyzers: Analyzer<'subject, 'props> seq -> Analyzer<'subject, 'props>

type internal LocatingTransformation =
    inherit SyntaxTreeTransformation

    new: options: TransformationOptions -> LocatingTransformation

    member Offset: Position QsNullable

module internal ContextRef =
    val local: value: 'a -> context: 'a ref -> IDisposable
