// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

open Microsoft.Quantum.QsCompiler.SymbolManagement
open System.Collections.Generic
open System.Threading
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Diagnostics

type Target(builtIns : NamespaceManager, capabilityLevel : CapabilityLevel) =
    member this.BuiltIns with get () = builtIns

    member this.CapabilityLevel with get () = capabilityLevel

    member this.ValidateSpecialization (spec : QsSpecialization) : List<DiagnosticItem> = 
        let diagnostics = new List<DiagnosticItem>()
        if spec.RequiredCapability > this.CapabilityLevel
        then diagnostics.Add(Error ErrorCode.UnexpectedCompilerException)
        diagnostics

    member this.ValidateSyntaxTree (ns : QsNamespace, cancellationToken : CancellationToken) : List<DiagnosticItem> = 
        new List<DiagnosticItem>()
