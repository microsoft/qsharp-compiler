// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

open Microsoft.Quantum.QsCompiler.SymbolManagement
open Microsoft.VisualStudio.LanguageServer.Protocol
open System.Collections.Generic
open System.Threading
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core

type TargetCapabilities =
    {
        CanDoIf : bool
        CanDoRepeat : bool
        CanMeasureAndContinue : bool
    }

type internal TargetValidationTransformation(target : Target) =
    inherit SyntaxTreeTransformation()

    let diagnostics = [] : Diagnostic list

    member this.Diagnostics with get () = new List<Diagnostic>(diagnostics)

and Target(builtIns : NamespaceManager, capabilities : TargetCapabilities) =
    member this.BuiltIns with get () = builtIns

    member this.Capabilities with get () = capabilities

    member this.ValidateSyntaxTree (ns : QsNamespace, cancellationToken : CancellationToken) : List<Diagnostic> = 
        let xformer = new TargetValidationTransformation(this)

        xformer.Transform ns |> ignore

        xformer.Diagnostics
