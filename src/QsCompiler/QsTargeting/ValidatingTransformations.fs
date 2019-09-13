// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Quantum.QsCompiler.Targeting

open Microsoft.Quantum.QsCompiler.SymbolManagement
open System.Collections.Generic
open Microsoft.Quantum.QsCompiler.SyntaxTree
open Microsoft.Quantum.QsCompiler.Transformations.Core
open Microsoft.Quantum.QsCompiler.Diagnostics

type internal TVExpressionXform() =
    inherit ExpressionTransformation()

type internal TVStatementKindXform() =
    inherit StatementKindTransformation()

    let scopeXformer = new TVScopeXform()
    let exprXformer = new TVExpressionXform()

    let diagnostics = new List<DiagnosticItem>()
    member this.Diagnostics with get () = diagnostics

    override this.ScopeTransformation x = scopeXformer.Transform x
    override this.ExpressionTransformation x = exprXformer.Transform x
    override this.TypeTransformation x = x
    override this.LocationTransformation x = x

and internal TVScopeXform() =
    inherit ScopeTransformation()

    let diagnostics = [] : DiagnosticItem list

    member this.Diagnostics with get () = new List<DiagnosticItem>(diagnostics)

type internal TVTreeXform() =
    inherit SyntaxTreeTransformation()

    let diagnostics = [] : DiagnosticItem list

    member this.Diagnostics with get () = new List<DiagnosticItem>(diagnostics)

