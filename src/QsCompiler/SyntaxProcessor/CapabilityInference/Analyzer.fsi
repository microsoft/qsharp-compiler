// Copyright (c) Microsoft Corporation.
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

type internal Pattern = { Capability: TargetCapability; Diagnose: Target -> QsCompilerDiagnostic option }

module internal Pattern =
    val concat: Pattern seq -> TargetCapability

type internal Analyzer<'Subject> = 'Subject -> Pattern seq

module internal Analyzer =
    val concat: analyzers: Analyzer<'Subject> seq -> Analyzer<'Subject>

type internal LocatingTransformation =
    inherit SyntaxTreeTransformation

    new: options: TransformationOptions -> LocatingTransformation

    member Offset: Position QsNullable

module internal ContextRef =
    val local: value: 'a -> context: 'a ref -> IDisposable
