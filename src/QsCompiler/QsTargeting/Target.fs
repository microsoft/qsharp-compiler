// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

open Microsoft.Quantum.QsCompiler.SymbolManagement
open System.Collections.Generic
open System.Threading
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Diagnostics

type Target(builtIns : NamespaceManager, capabilities : TargetCapabilities) =
    member this.BuiltIns with get () = builtIns

    member this.Capabilities with get () = capabilities

    member this.ValidateCallable (callable : QsCallable) : List<DiagnosticItem> = 
        let xformer = new TVScopeTransformation(this.Capabilities)

        let processSpecialization (spec : QsSpecialization) =
            match spec.Implementation with
            | Provided(_, scope) ->  xformer.Transform scope |> ignore
            | _ -> ()

        callable.Specializations |> Seq.iter processSpecialization

        xformer.Diagnostics

    member this.ValidateSyntaxTree (ns : QsNamespace, cancellationToken : CancellationToken) : List<DiagnosticItem> = 
        let xformer = new TVTreeTransformation(this.Capabilities)

        xformer.Transform ns |> ignore

        xformer.Diagnostics
